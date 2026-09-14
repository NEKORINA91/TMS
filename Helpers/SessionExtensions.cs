using Microsoft.AspNetCore.Http;
using System.Text.Json;
using TMS.Helpers;

namespace TMS.Helpers
{
    // In ASP.NET Core, session can only store strings/bytes.
    // These extension methods handle serialising SessionUser in and out.
    public static class SessionExtensions
    {
        private const string KEY = "LoggedInUser";

        public static void SetSessionUser(this ISession session, SessionUser user)
        {
            session.SetString(KEY, JsonSerializer.Serialize(user));
        }

        public static SessionUser GetSessionUser(this ISession session)
        {
            var value = session.GetString(KEY);
            if (string.IsNullOrEmpty(value)) return null;
            return JsonSerializer.Deserialize<SessionUser>(value);
        }

        public static void ClearSessionUser(this ISession session)
        {
            session.Remove(KEY);
        }

        public static bool IsLoggedIn(this ISession session)
        {
            return GetSessionUser(session) != null;
        }
    }
}
