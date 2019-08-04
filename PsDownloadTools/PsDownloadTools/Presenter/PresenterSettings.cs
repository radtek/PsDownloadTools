using PsDownloadTools.Helper;
using PsDownloadTools.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PsDownloadTools.Presenter
{
    class PresenterSettings
    {
        private readonly ViewSettings _view;
        private readonly Dictionary<String, String> langs = new Dictionary<String, String>();

        public PresenterSettings(ViewSettings view)
        {
            _view = view;
            Init();
        }

        private void Init()
        {
            ResourceDictionary resource = new ResourceDictionary { Source = new Uri(@"Resource\Languages\langs.xaml", UriKind.Relative) };
            foreach (Object key in resource.Keys)
            {
                Object value = resource[key.ToString()];
                if (value.ToString().Contains("Languages"))
                {
                    langs.Add(key.ToString(), value.ToString());
                }
            }

            Int32 index = langs.Keys.ToList().IndexOf(resource[SettingsHelper.Lang].ToString());
            index = index == -1 ? 0 : index;
            _view.TbExt = SettingsHelper.Exts;
            _view.SetCbLangSource(langs.Keys.ToList(), index);
            _view.BtnDownloadPath = SettingsHelper.DownloadPath;
        }

        public void ChangeLanguage(String key)
        {
            ResourceDictionary resource = new ResourceDictionary { Source = new Uri(langs[key], UriKind.Relative) };
            Application.Current.Resources.MergedDictionaries[0] = resource;
        }

        public void SelectPath()
        {
            using (System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDialog.SelectedPath = _view.BtnDownloadPath;
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _view.BtnDownloadPath = folderBrowserDialog.SelectedPath;
                }
                folderBrowserDialog.Dispose();
            }
        }

        public async void Save()
        {
            if (String.IsNullOrWhiteSpace(_view.TbExt))
            {
                ExceptionHelper.ShowErrorMsg(_view.TryFindResource("StrExtsEmpty") as String);
                return;
            }
            SettingsHelper.Exts = _view.TbExt;

            ResourceDictionary resource = new ResourceDictionary { Source = new Uri(@"Resource\Languages\langs.xaml", UriKind.Relative) };
            foreach (Object key in resource.Keys)
            {
                Object value = resource[key.ToString()];
                if (value.ToString().Equals(_view.CbLang))
                {
                    SettingsHelper.Lang = key.ToString();
                }
            }

            SettingsHelper.DownloadPath = _view.BtnDownloadPath;
            await Aria2Manager.GetInstance().SetDownloadPath();
        }
    }
}
