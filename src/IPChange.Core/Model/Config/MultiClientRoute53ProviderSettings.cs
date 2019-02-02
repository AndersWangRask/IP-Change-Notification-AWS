namespace IPChange.Core.Model.Config
{
    public class MultiClientRoute53ProviderSettings
    {
        public string R53ZoneId { get; set; }
        public string Name { get; set; }
        public bool UseEncryption { get; set; }
        public string EncryptionPassword { get; set; }
    }
}
