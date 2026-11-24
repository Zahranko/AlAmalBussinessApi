namespace AlAmalBusiness.Api.Push
{
    // "Push" section. ServiceAccountJson is the whole Firebase service-account
    // key file as one string (Firebase console -> Project settings -> Service
    // accounts -> Generate new private key). It is a secret: blank in the
    // tracked appsettings, injected on the VPS as Push__ServiceAccountJson
    // from the PROD_FIREBASE_SERVICE_ACCOUNT secret. Blank = push is off and
    // the app starts normally (same rule as Email and WhatsApp).
    public sealed class PushSettings
    {
        public const string SectionName = "Push";

        public bool Enabled { get; set; } = true;
        public string? ServiceAccountJson { get; set; }

        public bool IsUsable => Enabled && !string.IsNullOrWhiteSpace(ServiceAccountJson);
    }
}
