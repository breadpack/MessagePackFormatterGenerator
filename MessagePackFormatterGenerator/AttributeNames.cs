namespace MessagePackFormatterGenerator {
    public static class AttributeNames {
        public const string MessagePackObject = "MessagePack.MessagePackObjectAttribute";
        public const string IgnoreMember      = "MessagePack.IgnoreMemberAttribute";

        public const string JsonIgnore   = "Newtonsoft.Json.JsonIgnoreAttribute";
        public const string JsonProperty = "Newtonsoft.Json.JsonPropertyAttribute";

        public const string UnityJsonProperty = "Unity.Plastic.Newtonsoft.Json.JsonPropertyAttribute";
        public const string UnityJsonIgnore   = "Unity.Plastic.Newtonsoft.Json.JsonIgnoreAttribute";

        public const string NonSerialized = "System.NonSerializedAttribute";
        public const string Serializable  = "System.SerializableAttribute";

        public const string SerializeField = "UnityEngine.SerializeField";
    }
}