using AlAmalBusiness.Application.DTOs.WhatsApp;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Mammograms;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Mammograms
{
    // The register's WhatsApp follow-up reminder (added 2026-10-17). Once a
    // day (08:00, MammogramReminderScheduler), every row whose follow-up date
    // is today and has a phone gets the approved "radiologyreminde" template,
    // all of them in one bulk send through the hospital's WhatsApp platform —
    // one campaign there, under its Bulk send — which saves each patient as a
    // customer (name + MRN). The template's one variable is the row's
    // "نوع الفحص" (FollowUpExam), or, when that is blank, the exam read from
    // its recommendation ("6months u s" -> breast ultrasound).
    //
    // Exactly once: every row is claimed NotSent -> Queued by a conditional
    // UPDATE before the request goes out, so two workers can't both send it.
    // If the platform couldn't be reached, or refused the whole request, the
    // claims are handed back for the next run; if the answer was lost after it
    // may have queued (a timeout), the rows are failed instead — a missed
    // reminder is better than two. Editing the phone or follow-up date resets
    // a row to NotSent (MammogramService.ApplyUpdate): the only way to resend.
    public class MammogramReminderService : IMammogramReminderService
    {
        private const int MaxPerRun = 500;
        // Meta settles a status within minutes; after this a row is left as is.
        private static readonly TimeSpan SyncWindow = TimeSpan.FromDays(3);

        private readonly IMammogramRecordRepo _records;
        private readonly IWhatsAppSender _whatsApp;
        private readonly ILogger<MammogramReminderService> _logger;
        private readonly string _template;
        private readonly string? _language;

        public MammogramReminderService(
            IMammogramRecordRepo records,
            IWhatsAppSender whatsApp,
            IConfiguration config,
            ILogger<MammogramReminderService> logger)
        {
            _records = records;
            _whatsApp = whatsApp;
            _logger = logger;
            _template = config["MammogramReminders:Template"] is { Length: > 0 } t ? t : "radiologyreminde";
            _language = config["MammogramReminders:Language"] is { Length: > 0 } l ? l : null;
        }

        public async Task<MammogramReminderRunResult> SendDueAsync(CancellationToken ct = default)
        {
            var result = new MammogramReminderRunResult { Enabled = _whatsApp.IsEnabled };
            if (!_whatsApp.IsEnabled) return result;

            var now = AppClock.Now;
            var today = DateOnly.FromDateTime(now);
            var due = await _records.GetDueRemindersAsync(today, MaxPerRun);
            result.Due = due.Count;

            var claimed = new List<MammogramReminderRow>();
            foreach (var row in due)
                if (await _records.ClaimReminderAsync(row.Id, now)) claimed.Add(row);
            if (claimed.Count == 0) return result;

            var sent = await _whatsApp.SendBulkAsync(new WhatsAppBulkMessage
            {
                Name = $"Radiology follow-up reminders {today:yyyy-MM-dd}",
                Template = _template,
                Language = _language,
                Recipients = claimed.Select(row => new WhatsAppBulkRecipient
                {
                    Ref = row.Id.ToString(),
                    Phone = MammogramService.WhatsAppNumber(row.PhoneCountryCode, row.PhoneNumber)!,
                    Name = row.FullName,
                    FileNumber = row.PatientMrn,
                    Body = new List<string> { row.FollowUpExam ?? ExamText(row.Recommendation) }
                }).ToList()
            }, CancellationToken.None);
            // Not cancelled from here on: the claims must be settled whatever
            // happens to the app, or the rows would sit in Queued for good.

            if (!sent.Success)
            {
                var status = sent.Retryable ? MammogramReminderStatus.NotSent : MammogramReminderStatus.Failed;
                var error = sent.Retryable ? null : Clip(sent.Error);
                foreach (var row in claimed)
                    await _records.FinishReminderClaimAsync(row.Id, status, null, error);

                if (sent.Retryable) result.Deferred = claimed.Count;
                else result.Failed = claimed.Count;
                result.Error = sent.Error;
                _logger.LogWarning("Mammogram reminder bulk send of {Count} not accepted: {Error}", claimed.Count, sent.Error);
                return result;
            }

            result.CampaignId = sent.CampaignId;
            var lines = sent.Results.Where(l => l.Ref != null).GroupBy(l => l.Ref!).ToDictionary(g => g.Key, g => g.First());
            foreach (var row in claimed)
            {
                if (lines.TryGetValue(row.Id.ToString(), out var line) && line.MessageId != null)
                {
                    await _records.FinishReminderClaimAsync(row.Id, MammogramReminderStatus.Queued, line.MessageId, null);
                    result.Queued++;
                }
                else
                {
                    await _records.FinishReminderClaimAsync(row.Id, MammogramReminderStatus.Failed, null,
                        Clip(line?.Error) ?? "The WhatsApp platform didn't answer for this row.");
                    result.Failed++;
                }
            }

            _logger.LogInformation("Mammogram reminders: {Due} due, {Queued} queued, {Failed} failed (campaign {Campaign}).",
                result.Due, result.Queued, result.Failed, result.CampaignId);
            return result;
        }

        public async Task<int> SyncStatusesAsync(CancellationToken ct = default)
        {
            if (!_whatsApp.IsEnabled) return 0;

            var updated = 0;
            foreach (var row in await _records.GetRemindersInFlightAsync(AppClock.Now - SyncWindow))
            {
                if (ct.IsCancellationRequested) break;
                var status = await _whatsApp.GetStatusAsync(row.MessageId, ct);
                if (status == null) continue;

                var mapped = Map(status.Status);
                if (mapped == null || mapped == row.Status) continue;
                await _records.UpdateReminderStatusAsync(row.Id, row.MessageId, mapped.Value,
                    mapped == MammogramReminderStatus.Failed ? Clip(status.Error) ?? "Failed." : null);
                updated++;
            }
            return updated;
        }

        private static MammogramReminderStatus? Map(string platformStatus) => platformStatus switch
        {
            "queued" or "sending" => MammogramReminderStatus.Queued,
            "sent" => MammogramReminderStatus.Sent,
            "delivered" => MammogramReminderStatus.Delivered,
            "read" => MammogramReminderStatus.Read,
            "failed" or "cancelled" => MammogramReminderStatus.Failed,
            _ => null
        };

        private static string? Clip(string? error) =>
            string.IsNullOrWhiteSpace(error) ? null : error.Length <= 500 ? error : error[..500];

        // The template reads "نود تذكيرك بأن موعد {{1}} الخاص بك قد حان", so
        // {{1}} is the exam that is due, in Arabic. The recommendation column
        // is free text in the radiologists' shorthand ("6months u s",
        // "Routine annual screening mammogram ...", "BREAST MRI/6months u s"),
        // so it is matched by keyword; every exam it names is listed, and one
        // it doesn't recognise falls back to "the periodic exam".
        private static readonly Regex Ultrasound = new(@"\bu\.?\s?s\b|ultra\s?sound|sonar|\bbus\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Mammogram = new(@"mammo|\bmg\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex Mri = new(@"\bmri\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static string ExamText(string? recommendation)
        {
            var text = recommendation ?? "";
            var exams = new List<string>();
            if (Mammogram.IsMatch(text)) exams.Add("فحص الماموجرام");
            if (Ultrasound.IsMatch(text)) exams.Add("فحص الالتراساوند للثدي");
            if (Mri.IsMatch(text)) exams.Add("فحص الرنين المغناطيسي للثدي");
            return exams.Count == 0 ? "الفحص الدوري" : string.Join(" و", exams);
        }
    }
}
