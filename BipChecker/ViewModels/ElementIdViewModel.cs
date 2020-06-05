using Caliburn.Micro;
using System.Windows;
using static BipChecker.Utils.Util;

namespace BipChecker.ViewModels
{
    class ElementIdViewModel : Screen
    {
        public WindowResult Result { get; set; } = WindowResult.None;

        /// <summary>
        /// Typed-in element id, or empty string to pick element on screen.
        /// </summary>
        private string _elementId;

        public string ElementId
        {
            get { return _elementId; }
            set
            {
                _elementId = value;
                NotifyOfPropertyChange(() => ElementId);
                OkEnabled = int.TryParse(ElementId, out int id);
            }
        }

        public void PickElementClick(Window window)
        {
            _elementId = string.Empty;
            Result = WindowResult.Ok;

            if (window != null)
            {
                window.Close();
            }
        }

        public void OkClick(Window window)
        {
            Result = WindowResult.Ok;

            if (window != null)
            {
                window.Close();
            }
        }

        public void CancelClick()
        {
            _elementId = string.Empty;
            Result = WindowResult.Cancel;
        }

        private bool _okEnabled;

        public bool OkEnabled
        {
            get
            {
                return _okEnabled;
            }
            set
            {
                _okEnabled = value;
                NotifyOfPropertyChange(() => OkEnabled);
            }
        }
    }
}
