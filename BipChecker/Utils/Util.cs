using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BipChecker.ViewModels;
using BipChecker.Views;

namespace BipChecker.Utils
{
    static class Util
    {
        public enum WindowResult
        {
            None,
            Ok,
            Cancel
        }

        public const string Caption = "Built-in Parameter Checker";

        /// <summary>
        /// Format a real number and return its string representation.
        /// </summary>
        public static string RealString(double a)
        {
            return a.ToString("0.##");
        }

        /// <summary>
        /// Return a description string for a given element.
        /// </summary>
        public static string ElementDescription(Element e)
        {
            var description = (null == e.Category) ? e.GetType().Name : e.Category.Name;
            var familyInstance = e as FamilyInstance;

            if (null != familyInstance)
            {
                description += " '" + familyInstance.Symbol.Family.Name + "'";
            }

            if (null != e.Name)
            {
                description += " '" + e.Name + "'";
            }

            return description;
        }

        /// <summary>
        /// Return a description string including element id for a given element.
        /// </summary>
        public static string ElementDescription(Element element, bool includeId)
        {
            var description = ElementDescription(element);

            if (includeId)
            {
                description += " " + element.Id.IntegerValue.ToString();
            }

            return description;
        }

        /// <summary>
        /// Revit TaskDialog wrapper for a short informational message.
        /// </summary>
        public static void InfoMsg(string message)
        {
            Debug.WriteLine(message);
            TaskDialog.Show(Caption, message, TaskDialogCommonButtons.Ok);
        }

        /// <summary>
        /// MessageBox wrapper for error message.
        /// </summary>
        public static void ErrorMsg(string msg)
        {
            Debug.WriteLine(msg);

            var dialog = new TaskDialog(Caption);

            dialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
            dialog.MainInstruction = msg;

            dialog.Show();
        }

        /// <summary>
        /// MessageBox wrapper for question message.
        /// </summary>
        public static bool QuestionMsg(string msg)
        {
            Debug.WriteLine(msg);

            var dialog = new TaskDialog(Caption);

            dialog.MainIcon = TaskDialogIcon.TaskDialogIconNone;
            dialog.MainInstruction = msg;

            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Instance parameters");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Type parameters");

            return dialog.Show() == TaskDialogResult.CommandLink1;
        }

        public static Element GetSingleSelectedElementOrPrompt(UIDocument uidoc)
        {
            Element element = null;
            var ids = uidoc.Selection.GetElementIds();

            if (ids.Count == 1)
            {
                foreach (ElementId id in ids)
                {
                    element = uidoc.Document.GetElement(id);
                }
            }
            else
            {
                string elementIdString;
                var result = WindowResult.Ok;

                while (element == null && result == WindowResult.Ok)
                {
                    var elementIdSelectorViewModel = new ElementIdViewModel();
                    var elementIdSelector = new ElementIdView { DataContext = elementIdSelectorViewModel };
                    
                    elementIdSelector.ShowDialog();

                    result = elementIdSelectorViewModel.Result;
                    elementIdString = elementIdSelectorViewModel.ElementId;

                    if (result == WindowResult.Ok)
                    {
                        if (elementIdString.Length == 0)
                        {
                            try
                            {
                                var reference = uidoc.Selection.PickObject(ObjectType.Element, "Please pick an element");

                                element = uidoc.Document.GetElement(reference);
                            }
                            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                            {
                            }
                        }
                        else
                        {
                            if (int.TryParse(elementIdString, out int id))
                            {
                                var elementId = new ElementId(id);

                                element = uidoc.Document.GetElement(elementId);

                                if (element == null)
                                {
                                    ErrorMsg(string.Format("Invalid element id '{0}'.", elementIdString));
                                }
                            }
                            else
                            {
                                element = uidoc.Document.GetElement(elementIdString);

                                if (element == null)
                                {
                                    ErrorMsg(string.Format("Invalid element id '{0}'.", elementIdString));
                                }
                            }
                        }
                    }
                }
            }

            return element;
        }

        /// <summary>
        /// A selection filter for a specific System.Type.
        /// </summary>
        class TypeSelectionFilter : ISelectionFilter
        {
            Type _type;

            public TypeSelectionFilter(Type type)
            {
                _type = type;
            }

            /// <summary>
            /// Allow an element of the specified System.Type to be selected.
            /// </summary>
            /// <param name="element">A candidate element in selection operation.</param>
            /// <returns>Return true for specified System.Type, false for all other elements.</returns>
            public bool AllowElement(Element element)
            {
                return element.GetType().Equals(_type);
            }

            /// <summary>
            /// Allow all the reference to be selected
            /// </summary>
            /// <param name="refer">A candidate reference in selection operation.</param>
            /// <param name="point">The 3D position of the mouse on the candidate reference.</param>
            /// <returns>Return true to allow the user to select this candidate reference.</returns>
            public bool AllowReference(Reference reference, XYZ point)
            {
                return true;
            }
        }

        /// <summary>
        /// Helper to return parameter value as string.
        /// One can also use param.AsValueString() to
        /// get the user interface representation.
        /// </summary>
        public static string GetParameterValue(Parameter param)
        {
            string parameterString;

            switch (param.StorageType)
            {
                case StorageType.Double:
                    //
                    // the internal database unit for all lengths is feet.
                    // for instance, if a given room perimeter is returned as
                    // 102.36 as a double and the display unit is millimeters,
                    // then the length will be displayed as
                    // peri = 102.36220472440
                    // peri * 12 * 25.4
                    // 31200 mm
                    //
                    //s = param.AsValueString(); // value seen by user, in display units
                    //s = param.AsDouble().ToString(); // if not using not using LabUtils.RealString()
                    parameterString = RealString(param.AsDouble()); // raw database value in internal units, e.g. feet
                    break;

                case StorageType.Integer:
                    parameterString = param.AsInteger().ToString();
                    break;

                case StorageType.String:
                    parameterString = param.AsString();
                    break;

                case StorageType.ElementId:
                    parameterString = param.AsElementId().IntegerValue.ToString();
                    break;

                case StorageType.None:
                    parameterString = "?NONE?";
                    break;

                default:
                    parameterString = "?ELSE?";
                    break;
            }

            return parameterString;
        }

        static int _min_bic = 0;
        static int _max_bic = 0;

        static void SetMinAndMaxBuiltInCategory()
        {
            var values = Enum.GetValues(typeof(BuiltInCategory));
            _max_bic = values.Cast<int>().Max();
            _min_bic = values.Cast<int>().Min();
        }

        static string BuiltInCategoryString(int id)
        {
            if (_min_bic == 0)
            {
                SetMinAndMaxBuiltInCategory();
            }

            return (_min_bic < id && id < _max_bic) ? " " + ((BuiltInCategory)id).ToString() : string.Empty;
        }

        /// <summary>
        /// Helper to return parameter value as string, with additional
        /// support for element id to display the element type referred to.
        /// </summary>
        public static string GetParameterValue2(Parameter param, Document doc)
        {
            if (param.StorageType == StorageType.ElementId && doc != null)
            {
                var paramId = param.AsElementId();
                var id = paramId.IntegerValue;

                if (id < 0)
                {
                    return id.ToString() + BuiltInCategoryString(id);
                }
                else
                {
                    var element = doc.GetElement(paramId);

                    return ElementDescription(element, true);
                }
            }
            else
            {
                return GetParameterValue(param);
            }
        }
    }
}
