using AlAmalBusiness.Application.Services.Interface.Feedback;
using System;
using System.Security.Cryptography;

namespace AlAmalBusiness.Application.Services.Imp.Feedback
{
    // AMH-XXXXXX, drawn from a cryptographic RNG over an alphabet with the
    // visually ambiguous characters (I, O, 0, 1) removed — patients read these
    // off a phone screen and repeat them over the phone.
    public class ReferenceNumberGenerator : IReferenceNumberGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int Length = 6;

        public string Next()
        {
            Span<char> buffer = stackalloc char[Length];
            for (var i = 0; i < Length; i++)
                buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

            return $"AMH-{new string(buffer)}";
        }
    }
}
