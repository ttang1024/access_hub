namespace AccessHub.Cardholder.Api.Endpoints;

internal static class ApiRoutes
{
    public const string Sites = "/api/v1/sites";
    public const string Cardholders = "/api/v1/cardholders";
    public const string Cards = "/api/v1/cards";
    public const string AccessGroups = "/api/v1/access-groups";

    public static class Tags
    {
        public const string Sites = "Sites";
        public const string Doors = "Doors";
        public const string Cardholders = "Cardholders";
        public const string Cards = "Cards";
        public const string AccessGroups = "Access groups";
    }
}
