namespace RoomCast.Options
{
    public class AuthenticationOptions
    {
        public const string SectionName = "Authentication";

        public bool AutoLoginAfterRegistration { get; set; } = true;
    }
}
