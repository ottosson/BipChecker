using Autodesk.Revit.DB;
using BipChecker.Models;
using Caliburn.Micro;
using System.Collections.Generic;

namespace BipChecker.ViewModels
{
    public class BipCheckerListViewModel : Screen
    {
        // SortableBindingList<ParameterData>
        private BindableCollection<ParameterData> _bipList;
        private Element _element;

        public BipCheckerListViewModel(Element element, string description, List<ParameterData> data)
        {
            _element = element;
            Title = $"Built-in Parameters for {element.GetType()} ({description})";

            BIPList = new BindableCollection<ParameterData>();

            foreach (var item in data)
            {
                BIPList.Add(item);
            }
        }

        public BindableCollection<ParameterData> BIPList
        {
            get { return _bipList; }
            set
            {
                _bipList = value;
                NotifyOfPropertyChange(() => BIPList);
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
