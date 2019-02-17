using IoTControlKit.Models.Localization;
using IoTControlKit.Services.Database;
using IoTControlKit.ViewModels.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IoTControlKit.Services
{
    public class LocalizationService : BaseService
    {
        private static LocalizationService _uniqueInstance = null;
        private static object _lockObject = new object();

        public class TranslationCache
        {
            public TranslationCache()
            {
                LocalizationOriginalText = new Dictionary<string, long>();
                LocalizationTranslations = new Dictionary<long, Dictionary<long, string>>();
                AvailableCultures = new List<LocalizationCulture>();
            }

            public List<LocalizationCulture> AvailableCultures { get; set; }
            public Dictionary<string, long> LocalizationOriginalText { get; set; }
            public Dictionary<long, Dictionary<long, string>> LocalizationTranslations { get; set; }

            public void Clear()
            {
                LocalizationOriginalText.Clear();
                LocalizationTranslations.Clear();
                AvailableCultures.Clear();
            }
        }

        private TranslationCache _cache = null;

        private LocalizationService()
        {
            _cache = new TranslationCache();
            InitializeCache();
            LoadPredefinedTranslations();
            LocalizationDatabaseService.Instance.DatabaseRestored += Instance_DatabaseRestored;
        }

        private void Instance_DatabaseRestored(object sender, EventArgs e)
        {
            InitializeCache();
        }

        public static LocalizationService Instance
        {
            get
            {
                if (_uniqueInstance == null)
                {
                    lock (_lockObject)
                    {
                        if (_uniqueInstance == null)
                        {
                            _uniqueInstance = new LocalizationService();
                        }
                    }
                }
                return _uniqueInstance;
            }
        }

        public string GetManualMappingDirectory(string culture = "")
        {
            //for now fixed
            return "English (United States)";
        }

        private void InitializeCache()
        {
            lock (_cache)
            {
                _cache.Clear();
                LocalizationDatabaseService.Instance.Execute((db) =>
                {
                    var hs = new HashSet<string>();
                    var allOrgText = db.Fetch<LocalizationOriginalText>();
                    foreach (var ot in allOrgText)
                    {
                        hs.Add(ot.OriginalText);
                    }
                    foreach (var t in Data.OriginalText.Entries)
                    {
                        if (!hs.Contains(t))
                        {
                            var m = new LocalizationOriginalText();
                            m.OriginalText = t;
                            db.Save(m);
                        }
                    }

                    var lc = db.FirstOrDefault<LocalizationCulture>("where Name = 'en-US' COLLATE NOCASE");
                    if (lc == null)
                    {
                        lc = new LocalizationCulture();
                        lc.Name = "en-US";
                        lc.Description = "English (US)";
                        db.Save(lc);

                        lc = new LocalizationCulture();
                        lc.Name = "nl-NL";
                        lc.Description = "Nederlands";
                        db.Save(lc);
                    }

                    _cache.LocalizationOriginalText = db.Fetch<LocalizationOriginalText>().ToDictionary(i => i.OriginalText, i => i.Id);
                    _cache.AvailableCultures = db.Fetch<LocalizationCulture>();
                    foreach (var c in _cache.AvailableCultures)
                    {
                        _cache.LocalizationTranslations.Add(c.Id, db.Fetch<LocalizationTranslation>("where LocalizationCultureId = @0", c.Id).ToDictionary(i => i.LocalizationOriginalTextId, i => i.TranslatedText));
                    }
                });
            }
        }

        private void LoadPredefinedTranslations()
        {
            try
            {
                var d = Path.Combine(Program.HostingEnvironment.WebRootPath, "Data", "Translations");
                var fls = Directory.GetFiles(d, "*.xml");
                var allAvailableCultures = AvailableCultures;
                foreach (var f in fls)
                {
                    var cn = Path.GetFileNameWithoutExtension(f);
                    if ((from a in allAvailableCultures where string.Compare(cn, a.Name) == 0 select a).Any())
                    {
                        ImportLocalizationCultureText(File.ReadAllText(f));
                    }
                }
            }
            catch
            {
            }
        }

        public void Initialize()
        {
            CultureInfo ci = CurrentCultureInfo;
            if (ci == null)
            {
                ci = CultureInfo.CurrentUICulture; //Thread.CurrentThread.CurrentUICulture;
                CurrentCultureInfo = ci;
                CurrentCulture = GetLocalizationCulture(ci.Name);
            }

            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;
        }

        public LocalizationCulture CurrentCulture
        {
            get
            {
                var result = System.Web.HttpContext.Current.Session.GetObjectFromJson<LocalizationCulture>("LocalizationService.CurrentCulture");
                //var result = contextAccessor.HttpContext.Session["LocalizationService.CurrentCulture"] as LocalizationCulture;
                return result;
            }
            set
            {
                System.Web.HttpContext.Current.Session.SetObjectAsJson("LocalizationService.CurrentCulture", value);
                //HttpContext.Current.Session["LocalizationService.CurrentCulture"] = value;
            }
        }

        public CultureInfo CurrentCultureInfo
        {
            get
            {
                var name = System.Web.HttpContext.Current.Session.GetObjectFromJson<string>("LocalizationService.CurrentCultureInfo");
                if (!string.IsNullOrEmpty(name))
                    return new System.Globalization.CultureInfo(name);
                else
                    return null;
            }
            set
            {
                System.Web.HttpContext.Current.Session.SetObjectAsJson("LocalizationService.CurrentCultureInfo", value?.Name);
            }
        }

        public LocalizationCulture GetLocalizationCulture(string culture)
        {
            LocalizationCulture result = null;
            lock (this)
            {
                LocalizationDatabaseService.Instance.Execute((db) =>
                {
                    result = db.FirstOrDefault<LocalizationCulture>("where Name = @0 COLLATE NOCASE", culture);
                });
            }
            return result;
        }

        public string Translate(string text)
        {
            var result = text;
            if (CurrentCulture != null)
            {
                lock (_cache)
                {
                    Dictionary<long, string> table;
                    if (_cache.LocalizationTranslations.TryGetValue(CurrentCulture.Id, out table))
                    {
                        string translatedText = null;

                        long originalTextId = 0;
                        if (_cache.LocalizationOriginalText.TryGetValue(text, out originalTextId))
                        {
                            table.TryGetValue(originalTextId, out translatedText);
                        }
                        else
                        {
                            LocalizationDatabaseService.Instance.Execute((db) =>
                            {
                                var m = new LocalizationOriginalText();
                                m.OriginalText = text;
                                db.Save(m);
                                _cache.LocalizationOriginalText.Add(text, m.Id);
                            });
                        }

                        result = string.IsNullOrEmpty(translatedText) ? text : translatedText;
                    }
                }
            }
            return result;
        }

        public string this[string key]
        {
            get
            {
                return Translate(key);
            }
        }

        public void SaveLocalizationCulture(LocalizationCulture m)
        {
            lock (_cache)
            {
                LocalizationDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
                {
                    if (m.Id > 0)
                    {
                        var c = (from a in _cache.AvailableCultures where a.Id == m.Id select a).FirstOrDefault();
                        if (c != null)
                        {
                            //not alowed to change name
                            m.Name = c.Name;
                        }
                        else
                        {
                            throw new Exception("Does not exist");
                        }
                    }
                    else if ((from a in _cache.AvailableCultures where a.Name == m.Name select a).Any())
                    {
                        throw new Exception("Already exists");
                    }
                    else
                    {
                        var c = new System.Globalization.CultureInfo(m.Name);
                    }

                    db.Save(m);
                    if (!_cache.LocalizationTranslations.TryGetValue(m.Id, out var table))
                    {
                        _cache.LocalizationTranslations.Add(m.Id, new Dictionary<long, string>());
                    }
                    _cache.AvailableCultures = db.Fetch<LocalizationCulture>();
                });
            }
        }

        public void DeleteLocalizationCulture(LocalizationCulture m)
        {
            if (m.Name != "en-US")
            {
                lock (_cache)
                {
                    LocalizationDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
                    {
                        if (_cache.LocalizationTranslations.TryGetValue(m.Id, out var table))
                        {
                            db.Execute("delete from LocalizationTranslation where LocalizationCultureId=@0", m.Id);
                            db.Delete(m);
                            _cache.LocalizationTranslations.Remove(m.Id);

                            _cache.AvailableCultures = db.Fetch<LocalizationCulture>();
                        }
                    });
                }
            }
        }

        public void SaveLocalizationTranslation(LocalizationTranslationViewModelItem item)
        {
            lock (_cache)
            {
                LocalizationDatabaseService.Instance.Execute((db) =>
                {
                    Dictionary<long, string> table;
                    if (_cache.LocalizationTranslations.TryGetValue(item.CultureId, out table) && db.FirstOrDefault<LocalizationOriginalText>("where Id=@0", item.OriginalTextId) != null)
                    {
                        var m = db.FirstOrDefault<LocalizationTranslation>("where LocalizationCultureId=@0 and LocalizationOriginalTextId=@1", item.CultureId, item.OriginalTextId);
                        if (m == null)
                        {
                            m = new LocalizationTranslation();
                            m.LocalizationCultureId = item.CultureId;
                            m.LocalizationOriginalTextId = item.OriginalTextId;
                        }
                        table[item.OriginalTextId] = item.TranslatedText;
                        m.TranslatedText = item.TranslatedText;
                        db.Save(m);
                    }
                });
            }
        }

        public LocalizationTranslationViewModel GetLocalizationTranslations(int page, int pageSize, long cultureId, string filterOrg = "", string filterTrans = "", string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("LocalizationOriginalText.Id as Id")
                .Append(", LocalizationOriginalText.OriginalText")
                .Append(", LocalizationTranslation.TranslatedText as TranslatedText")
                .Append($", {cultureId} as CultureId")
                .Append(", LocalizationOriginalText.Id as OriginalTextId")
                .Append(", LocalizationCulture.Id as LocalizationCultureId")
                .Append(", LocalizationCulture.Name as LocalizationCultureName")
                .From("LocalizationOriginalText")
                .LeftJoin("LocalizationTranslation").On("LocalizationOriginalText.Id = LocalizationTranslation.LocalizationOriginalTextId and LocalizationTranslation.LocalizationCultureId=@0", cultureId)
                .LeftJoin("LocalizationCulture").On("LocalizationTranslation.LocalizationCultureId = LocalizationCulture.Id")
                .Where("1=1");
            if (!string.IsNullOrEmpty(filterOrg))
            {
                sql = sql.Append("and LocalizationOriginalText.OriginalText like @0", string.Format("%{0}%", filterOrg));
            }
            if (!string.IsNullOrEmpty(filterTrans))
            {
                sql = sql.Append("and LocalizationTranslation.TranslatedText like @0", string.Format("%{0}%", filterTrans));
            }
            if (!string.IsNullOrEmpty(sortOn))
            {
                sortOn = string.Format("CAST({0} as NVarchar(1000))", sortOn);
            }
            var result = LocalizationDatabaseService.Instance.GetPage<LocalizationTranslationViewModel, LocalizationTranslationViewModelItem>(page, pageSize, sortOn, sortAsc, "CAST(OriginalText as NVarchar(1000))", sql);
            return result;
        }

        public LocalizationCulture[] AvailableCultures
        {
            get
            {
                LocalizationCulture[] result;
                lock (_cache)
                {
                    result = _cache.AvailableCultures.ToArray();
                }
                return result;
            }
        }

        public LocalizationCultureViewModel GetLocalizationCultures(int page, int pageSize, string sortOn = "", bool sortAsc = true)
        {
            var sql = NPoco.Sql.Builder.Select("LocalizationCulture.*")
                .From("LocalizationCulture");
            return LocalizationDatabaseService.Instance.GetPage<LocalizationCultureViewModel, LocalizationCultureViewModelItem>(page, pageSize, sortOn, sortAsc, "Name", sql);
        }

        public LocalizationCulture ImportLocalizationCultureText(string xml)
        {
            LocalizationCulture result = null;
            try
            {
                var xdoc = XDocument.Parse(xml);
                if (xdoc != null)
                {
                    var cultureCode = xdoc.Root.Attribute("cultureCode").Value;
                    var cultureDescription = xdoc.Root.Attribute("cultureDescription").Value;

                    lock (_cache)
                    {
                        LocalizationDatabaseService.Instance.ExecuteWithinTransaction((db, session) =>
                        {
                            result = (from a in AvailableCultures where string.Compare(cultureCode, a.Name) == 0 select a).FirstOrDefault();
                            var isNew = result == null;
                            if (isNew)
                            {
                                isNew = true;
                                var cultureInfo = new CultureInfo(cultureCode);
                                result = new LocalizationCulture();
                                result.Description = cultureDescription;
                                result.Name = cultureCode;
                                db.Save(result);
                            }

                            var textList = xdoc.Root.Elements("string");
                            if (!_cache.LocalizationTranslations.TryGetValue(result.Id, out var translationTable))
                            {
                                translationTable = new Dictionary<long, string>();
                            }
                            foreach (var textNode in textList)
                            {
                                var originalText = textNode.Attribute("originalText").Value;
                                var translatedText = textNode.Attribute("translatedText").Value;

                                if (!string.IsNullOrEmpty(translatedText))
                                {
                                    if (_cache.LocalizationOriginalText.TryGetValue(originalText, out var orgId))
                                    {
                                        if (isNew || !translationTable.TryGetValue(orgId, out var curTranslationText))
                                        {
                                            translationTable.Add(orgId, translatedText);

                                            var newTranslation = new LocalizationTranslation();
                                            newTranslation.LocalizationCultureId = result.Id;
                                            newTranslation.LocalizationOriginalTextId = orgId;
                                            newTranslation.TranslatedText = translatedText;
                                            db.Save(newTranslation);
                                        }
                                        else if (curTranslationText != translatedText)
                                        {
                                            translationTable[orgId] = translatedText;

                                            var m = db.FirstOrDefault<LocalizationTranslation>("where LocalizationCultureId=@0 and LocalizationOriginalTextId=@1", result.Id, orgId);
                                            m.TranslatedText = translatedText;
                                            db.Save(m);
                                        }
                                    }
                                }
                            }

                            //if all went ok
                            if (!_cache.LocalizationTranslations.TryGetValue(result.Id, out var table))
                            {
                                _cache.LocalizationTranslations.Add(result.Id, translationTable);
                                _cache.AvailableCultures = db.Fetch<LocalizationCulture>();
                            }
                        });
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        public string ExportLocalizationCultureText(long id)
        {
            var item = (from a in AvailableCultures where a.Id == id select a).FirstOrDefault();

            var sqlOriginalText = NPoco.Sql.Builder.Select("LocalizationOriginalText.*")
                .From("LocalizationOriginalText");

            var sqlTranslatedText = NPoco.Sql.Builder.Select("LocalizationTranslation.*")
                .From("LocalizationTranslation")
                .Where("LocalizationCultureId = @0", item.Id);

            XDocument xDocument = null;
            LocalizationDatabaseService.Instance.Execute((db) =>
            {
                var originalText = db.Fetch<LocalizationOriginalText>(sqlOriginalText).OrderBy(x => x.OriginalText).ToList();
                var translatedText = db.Fetch<LocalizationTranslation>(sqlTranslatedText).ToList();

                xDocument = new XDocument(
                new XElement("translated",
                    new XAttribute("cultureCode", item.Name),
                    new XAttribute("cultureDescription", item.Description),
                        originalText.Select(x => new XElement("string",
                            new XAttribute("originalText", x.OriginalText),
                            new XAttribute("translatedText", translatedText.Where(o => o.LocalizationOriginalTextId == x.Id).FirstOrDefault() == null ?
                                    string.Empty : !string.IsNullOrEmpty(translatedText.Where(o => o.LocalizationOriginalTextId == x.Id).FirstOrDefault().TranslatedText) ? translatedText.Where(o => o.LocalizationOriginalTextId == x.Id).FirstOrDefault().TranslatedText : string.Empty))
                        )
                    )
                );
            });
            return xDocument.ToString();
        }

    }

}
