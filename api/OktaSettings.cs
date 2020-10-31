namespace api
{
    public class OktaSettings
    {
        public string ScopePrefix { get; set; }
        public string GroupPrefix { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public override string ToString()
        {
            return $"Iss: '{Issuer}' Aud: '{Audience}'";
        }
    }
}
