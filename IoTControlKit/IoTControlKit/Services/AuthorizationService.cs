using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IoTControlKit.Services
{
    public class AuthorizationService : BaseService
    {
        private static AuthorizationService _uniqueInstance = null;
        private static object _lockObject = new object();

        private Models.Settings.User _guestUser;
        private Dictionary<long, string> _features;
        private Dictionary<long, HashSet<string>> _roleFeatures;

        public enum Feature
        {
            UserManagement,
            Logging,
            ChangeLocalization
        };

        public static Dictionary<Feature, string> FeatureDescription = new Dictionary<Feature, string>()
        {
            {Feature.UserManagement, "User Management" }
            , {Feature.Logging, "Access logging" }
            , {Feature.ChangeLocalization, "Change translations" }
        };

        private AuthorizationService()
        {
            InitCache();
        }

        public static AuthorizationService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new AuthorizationService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        private void InitCache()
        {
            Database.SettingsDatabaseService.Instance.Execute((db) =>
            {
                _features = db.Fetch<Models.Settings.Feature>().ToDictionary(x => x.Id, x => x.Name);
                _guestUser = db.Query<Models.Settings.User>().Where(x => x.Name == "Guest").FirstOrDefault();

                _roleFeatures = db.Fetch<Models.Settings.Role>().ToDictionary(x => x.Id, x => new HashSet<string>());
                foreach (var role in _roleFeatures)
                {
                    var roleFratures = db.Query<Models.Settings.RoleFeature>().Where(x => x.RoleId == role.Key).ToList();
                    foreach (var roleFrature in roleFratures)
                    {
                        role.Value.Add(_features[roleFrature.FeatureId]);
                    }
                }
            });
        }

        public Models.Settings.User SignOut()
        {
            CurrentUser = _guestUser;
            IsVmiAdmin = false;
            return _guestUser;
        }

        public Models.Settings.User SignIn(string name, string password)
        {
            var result = _guestUser;
            Database.SettingsDatabaseService.Instance.Execute((db) =>
            {
                var usr = db.Fetch<Models.Settings.User>("where Name like @0", name).FirstOrDefault();
                if (usr != null)
                {
                    if (HashPassword(password) == usr.PasswordHash)
                    {
                        result = usr;
                        CurrentUser = usr;
                        IsVmiAdmin = false;
                    }
                    else
                    {
                        NotificationService.Instance.AddErrorMessage($"Invalid password");
                        LoggerService.Instance.LogError(null, $"Invalid password for user '{usr.Name}'");
                    }
                }
                else
                {
                    NotificationService.Instance.AddErrorMessage($"Unknown user");
                    LoggerService.Instance.LogError(null, $"Invalid user '{name}'");
                }
            });
            return result;
        }

        public string GetCookieDataForUser(Models.Settings.User user)
        {
            var result = "";
            //todo
            //for now just easy....
            Newtonsoft.Json.JsonConvert.SerializeObject(user);
            return result;
        }

        public Models.Settings.User InitializeSession(string cookieData)
        {
            var result = System.Web.HttpContext.Current.Session.GetObjectFromJson<Models.Settings.User>("AuthorizationService.CurrentUser");
            if (result == null)
            {
                result = SetUserFromCookieData(cookieData);
            }
            return result;
        }

        private Models.Settings.User SetUserFromCookieData(string cookieData)
        {
            var result = _guestUser;
            if (!string.IsNullOrEmpty(cookieData))
            {
                //todo
                //for now just easy....
                try
                {
                    var usr = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Settings.User>(cookieData);
                    if (usr != null)
                    {
                        Database.SettingsDatabaseService.Instance.Execute((db) =>
                        {
                            var m = db.Query<Models.Settings.User>().Where(x => x.Id == usr.Id).FirstOrDefault();
                            if (m != null && m.PasswordHash == usr.PasswordHash && m.Name == usr.Name)
                            {
                                result = m;
                            }
                        });
                    }
                }
                catch
                {
                    //illegal data
                }
            }
            CurrentUser = result;
            return result;
        }

        public bool ValidateVmiPassword(string password)
        {
            var result = true;
            result = (password == Environment.MachineName);
            if (result)
            {
                IsVmiAdmin = true;
            }
            return result;
        }

        public bool IsVmiAdminOrValidPassword(string password)
        {
#if RELEASE
            var result = IsVmiAdmin;
            if (!result)
            {
                result = ValidateVmiPassword(password);
            }
            return result;
#else
            return true;
#endif
        }

        public bool IsVmiAdmin
        {
            get
            {
                return System.Web.HttpContext.Current.Session.GetObjectFromJson<bool>("AuthorizationService.IsVmiAdmin");
            }
            set
            {
                System.Web.HttpContext.Current.Session.SetObjectAsJson("AuthorizationService.IsVmiAdmin", value);
            }
        }

        public Models.Settings.User CurrentUser
        {
            get
            {
                var result = System.Web.HttpContext.Current.Session.GetObjectFromJson<Models.Settings.User>("AuthorizationService.CurrentUser");
                if (result == null)
                {
                    result = _guestUser;
                }
                return result;
            }
            set
            {
                System.Web.HttpContext.Current.Session.SetObjectAsJson("AuthorizationService.CurrentUser", value);
            }
        }

        public bool FeatureAllowed(params Feature[] feature)
        {
            return FeatureAllowed(CurrentUser, feature);
        }

        public bool FeatureAllowed(Models.Settings.User user, params Feature[] feature)
        {
            bool result = false;
            lock (this)
            {
                if (user != null)
                {
                    foreach (var f in feature)
                    {
                        result |= (_roleFeatures[user.RoleId]?.Contains(f.ToString()) == true);
                    }
                }
            }
            return result;
        }

        public List<string> GetRoleNames()
        {
            List<string> result = null;
            Database.SettingsDatabaseService.Instance.Execute((db) =>
            {
                result = db.Fetch<string>("select Name from Role order by Name");
            });
            return result;
        }

        public List<Models.Settings.Feature> GetFeatures()
        {
            List<Models.Settings.Feature> result = null;
            Database.SettingsDatabaseService.Instance.Execute((db) =>
            {
                result = db.Fetch<Models.Settings.Feature>().OrderBy(x => x.Name).ToList();
            });
            return result;
        }

        public ViewModels.BaseViewModel GetFeaturesForRole(long roleId)
        {
            var result = CompleteViewModel(null);
            result.ResultSuccess = true;
            Database.SettingsDatabaseService.Instance.Execute((db) =>
            {
                var sql = NPoco.Sql.Builder.Select("Feature.Id")
                    .Append(",RoleFeature.RoleId as RoleId")
                    .From("Feature")
                    .LeftJoin("RoleFeature").On("Feature.Id=RoleFeature.FeatureId and RoleFeature.RoleId=@0", roleId);
                result.ResultData = db.Fetch<dynamic>(sql);
            });
            return result;
        }

        public void CheckMinimumAuthorizations(NPoco.Database db)
        {
            var sql = NPoco.Sql.Builder.Select("User.*")
                .Append(",RoleFeature.RoleId as RoleId")
                .From("User")
                .InnerJoin("Role").On("User.RoleId=Role.Id")
                .InnerJoin("RoleFeature").On("Role.Id=RoleFeature.RoleId")
                .InnerJoin("Feature").On("RoleFeature.FeatureId=Feature.Id")
                .Where("Feature.Name=@0 and User.Enabled=1", Feature.UserManagement.ToString());
            if (!db.Query<Models.Settings.User>(sql).Any())
            {
                throw new Exception(_T("Operation noit allowed"));
            }
        }

        public ViewModels.BaseViewModel SetFeatureForRole(long roleId, long featureId, bool enabled)
        {
            var result = CompleteViewModel(null);
            lock (this)
            {
                result.ResultSuccess = Database.SettingsDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
                {
                    var item = db.Query<Models.Settings.RoleFeature>().Where(x => x.RoleId == roleId && x.FeatureId == featureId).FirstOrDefault();
                    if (item == null && enabled)
                    {
                        item = new Models.Settings.RoleFeature();
                        item.FeatureId = featureId;
                        item.RoleId = roleId;
                        db.Save(item);
                        CheckMinimumAuthorizations(db);
                        _roleFeatures[roleId].Add(_features[featureId]);
                    }
                    else if (item != null && !enabled)
                    {
                        db.Delete(item);
                        CheckMinimumAuthorizations(db);
                        _roleFeatures[roleId].Remove(_features[featureId]);
                    }
                });
            }
            return result;
        }


        public ViewModels.Authorization.UsersViewModel GetUsers(int page, int pageSize, string filterName = "", string filterDisplayName = "", string filterRole = "", string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("*")
                .Append(",Role.Name as RoleName")
                .From("User")
                .InnerJoin("Role").On("User.RoleId=Role.Id")
                .Where("1=1");
            if (!string.IsNullOrEmpty(filterName))
            {
                sql = sql.Append("and User.Name like @0", $"%{filterName}%");
            }
            if (!string.IsNullOrEmpty(filterDisplayName))
            {
                sql = sql.Append("and User.DisplayName like @0", $"%{filterDisplayName}%");
            }
            if (!string.IsNullOrEmpty(filterRole))
            {
                sql = sql.Append("and Role.Name like @0", $"%{filterRole}%");
            }
            return Database.SettingsDatabaseService.Instance.GetPage<ViewModels.Authorization.UsersViewModel, ViewModels.Authorization.UsersViewModelItem>(page, pageSize, sortOn, sortAsc, "User.Name", sql);
        }

        public ViewModels.Authorization.RolesViewModel GetRoles(int page, int pageSize, string filterName = "", string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("*")
                .From("Role")
                .Where("1=1");
            if (!string.IsNullOrEmpty(filterName))
            {
                sql = sql.Append("and Role.Name like @0", $"%{filterName}%");
            }
            return Database.SettingsDatabaseService.Instance.GetPage<ViewModels.Authorization.RolesViewModel, ViewModels.Authorization.RolesViewModelItem>(page, pageSize, sortOn, sortAsc, "Name", sql);
        }

        public string HashPassword(string password)
        {
            string result;
            var data = Encoding.UTF8.GetBytes(password);
            var sha = new SHA256CryptoServiceProvider();
            data = sha.ComputeHash(data);
            result = System.Convert.ToBase64String(data);
            return result;
        }

        public ViewModels.BaseViewModel SaveUser(Models.Settings.User item, string roleName)
        {
            var result = CompleteViewModel(null);
            result.ResultSuccess = Database.SettingsDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
            {
                //todö: extra checks to prevent locking yourself out
                var selectedRole = db.Query<Models.Settings.Role>().Where(x => x.Name == roleName).First();
                var org = db.Query<Models.Settings.User>().Where(x => x.Id == item.Id).FirstOrDefault();
                if (item.CheckForNewName(db, item.Name) && org?.Name != "Guest")
                {
                    item.RoleId = selectedRole.Id;
                    if (string.IsNullOrEmpty(item.PasswordHash))
                    {
                        item.PasswordHash = org.PasswordHash;
                    }
                    else
                    {
                        item.PasswordHash = HashPassword(item.PasswordHash);
                    }
                    db.Save(item);
                    CheckMinimumAuthorizations(db);
                }
                else
                {
                    NotificationService.Instance.AddSuccessMessage("User already exists");
                }
            });
            if (result.ResultSuccess == true)
            {
                NotificationService.Instance.AddSuccessMessage("Data saved.");
            }
            else
            {
                NotificationService.Instance.AddErrorMessage("Error saving data.");
            }
            return result;
        }

        public ViewModels.BaseViewModel DeleteUser(Models.Settings.User item)
        {
            var result = CompleteViewModel(null);
            result.ResultSuccess = Database.SettingsDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
            {
                var org = db.Query<Models.Settings.User>().Where(x => x.Id == item.Id).FirstOrDefault();
                if (org != null && org.Id != CurrentUser.Id)
                {
                    if (org.Name != "Guest")
                    {
                        db.Delete(item);
                        CheckMinimumAuthorizations(db);
                    }
                }
            });
            if (result.ResultSuccess == true)
            {
                NotificationService.Instance.AddSuccessMessage("Data deleted.");
            }
            else
            {
                NotificationService.Instance.AddErrorMessage("Error deleting data.");
            }
            return result;
        }

        public ViewModels.BaseViewModel SaveRole(Models.Settings.Role item)
        {
            var result = CompleteViewModel(null);
            lock (this)
            {
                result.ResultSuccess = Database.SettingsDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
                {
                    var org = db.Query<Models.Settings.Role>().Where(x => x.Id == item.Id).FirstOrDefault();
                    if (item.CheckForNewName(db, item.Name) && org?.Name != "Guest" && org?.Name != "Admin")
                    {
                        db.Save(item);
                        CheckMinimumAuthorizations(db);
                        if (org == null)
                        {
                            _roleFeatures.TryAdd(item.Id, new HashSet<string>());
                        }
                    }
                    else
                    {
                        NotificationService.Instance.AddSuccessMessage("Role already exists");
                    }
                });
            }
            if (result.ResultSuccess == true)
            {
                NotificationService.Instance.AddSuccessMessage("Data saved.");
            }
            else
            {
                NotificationService.Instance.AddErrorMessage("Error saving data.");
            }
            return result;
        }

        public ViewModels.BaseViewModel DeleteRole(Models.Settings.Role item)
        {
            var result = CompleteViewModel(null);
            lock (this)
            {
                result.ResultSuccess = Database.SettingsDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
                {
                    var org = db.Query<Models.Settings.Role>().Where(x => x.Id == item.Id).FirstOrDefault();
                    if (org != null)
                    {
                        if (org.Name != "Guest" && org.Name != "Admin"
                        && db.Query<Models.Settings.User>().Where(x => x.RoleId == item.Id).Count() == 0)
                        {
                            var allfeatureRoles = db.Query<Models.Settings.RoleFeature>().Where(x => x.RoleId == item.Id).ToList();
                            foreach (var fr in allfeatureRoles)
                            {
                                db.Delete(fr);
                            }
                            db.Delete(item);
                            CheckMinimumAuthorizations(db);
                            _roleFeatures.Remove(item.Id);
                        }
                        else
                        {
                            throw new Exception(_T("Operation not allowed"));
                        }
                    }
                });
            }
            if (result.ResultSuccess == true)
            {
                NotificationService.Instance.AddSuccessMessage("Data saved.");
            }
            else
            {
                NotificationService.Instance.AddErrorMessage("Error saving data.");
            }
            return result;
        }

    }

}
