namespace OrderManagement.Api.Configuration;

public static class ApiConstants
{
    public static class Environments
    {
        public const string Testing = "Testing";
        public const string LoadTesting = "LoadTesting";
    }

    public static class RateLimitPolicies
    {
        public const string WritePolicy = "write-policy";
    }
}