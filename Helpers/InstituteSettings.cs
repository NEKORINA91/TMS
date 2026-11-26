using System.Text.Json;

namespace TMS.Helpers
{
    public class InstituteInfo
    {
        public string Name { get; set; } = "Tuition Management System";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class InstituteSettings
    {
        private readonly string _file;
        private readonly object _lock = new object();
        private InstituteInfo _info;

        public InstituteSettings(IWebHostEnvironment env)
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dir);
            _file = Path.Combine(dir, "institute.json");
            _info = Load();
        }

        private InstituteInfo Load()
        {
            try
            {
                if (File.Exists(_file))
                    return JsonSerializer.Deserialize<InstituteInfo>(File.ReadAllText(_file)) ?? new InstituteInfo();
            }
            catch { }
            return new InstituteInfo();
        }

        public string Name => _info.Name;
        public string Phone => _info.Phone;
        public string Email => _info.Email;

        public void Save(string name, string phone, string email)
        {
            lock (_lock)
            {
                _info = new InstituteInfo { Name = name, Phone = phone, Email = email };
                File.WriteAllText(_file, JsonSerializer.Serialize(_info));
            }
        }
    }
}