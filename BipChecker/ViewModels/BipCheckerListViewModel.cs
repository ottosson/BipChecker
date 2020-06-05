using Autodesk.Revit.DB;
using BipChecker.Models;
using Caliburn.Micro;
using System.Collections.Generic;

namespace BipChecker.ViewModels
{
    public class BipCheckerListViewModel : Screen
    {
        // SortableBindingList<ParameterData>
        private BindableCollection<ParameterData> _licenseListRows;
        private Element _element;

        public BipCheckerListViewModel(Element element, string description, List<ParameterData> data)
        {
            _element = element;
            Title = $"Built-in Parameters for {element.GetType()} ({description})";

            LicenseListRows = new BindableCollection<ParameterData>();

            foreach (var item in data)
            {
                LicenseListRows.Add(item);
            }
        }

        public BindableCollection<ParameterData> LicenseListRows
        {
            get { return _licenseListRows; }
            set
            {
                _licenseListRows = value;
                NotifyOfPropertyChange(() => LicenseListRows);
            }
        }

        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                NotifyOfPropertyChange(() => Title);

            }
        }

    }
}
