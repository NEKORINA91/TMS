using System;
using System.Security.Cryptography;
using System.Text;

namespace TMS.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("TuitionPro_Salt_" + password));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool Verify(string password, string hash)
        {
            return Hash(password) == hash;
        }
    }

    [Serializable]
    public class SessionUser
    {
        public int    UserId    { get; set; }
        public string SystemId  { get; set; }
        public string FullName  { get; set; }
        public string Email     { get; set; }
        public string Role      { get; set; }
        public int    ProfileId { get; set; }
    }

    public static class SystemIdHelper
    {
        public static string Generate(string role, int nextNumber)
        {
            string prefix;
            if      (role == "SuperAdmin") prefix = "ADM";
            else if (role == "Admin")      prefix = "ADM";
            else if (role == "Teacher")    prefix = "TCH";
            else if (role == "Student")    prefix = "STD";
            else                           prefix = "USR";
            return string.Format("{0}-{1:D3}", prefix, nextNumber);
        }
    }

    public static class ReceiptHelper
    {
        public static string Generate(int feeId)
        {
            return string.Format("RC-{0}", 1000 + feeId);
        }
    }
}
