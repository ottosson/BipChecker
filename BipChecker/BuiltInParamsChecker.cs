using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BipChecker.Models;
using BipChecker.Utils;
using BipChecker.ViewModels;
using BipChecker.Views;

namespace BipChecker
{
    /// <summary>
    /// List all accessible built-in parameters on 
    /// a selected element in a DataGridView.
    /// Todo: add support for shared parameters also.
    /// </summary>
    //[Transaction(TransactionMode.ReadOnly)]
    [Transaction(TransactionMode.Manual)]
    public class BuiltInParamsChecker : IExternalCommand
    {
        const string _type_prompt = "This element {0}. Would you like to see the instance parameters or the type parameters?";

        #region Contained in ParameterSet Collection

        /// <summary>
        /// Return BuiltInParameter id for a given parameter,
        /// assuming it is a built-in parameter.
        /// </summary>
        static BuiltInParameter BipOf(Parameter p)
        {
            return (p.Definition as InternalDefinition).BuiltInParameter;
        }

        /// <summary>
        /// Check whether two given parameters represent
        /// the same parameter, i.e. shared parameters
        /// have the same GUID, others the same built-in
        /// parameter id.
        /// </summary>
        static bool IsSameParameter(Parameter p, Parameter q)
        {
            return (p.IsShared == q.IsShared) && (p.IsShared ? p.GUID.Equals(q.GUID) : BipOf(p) == BipOf(q));
        }

        /// <summary>
        /// Return true if the given element parameter 
        /// retrieved by  get_parameter( BuiltInParameter ) 
        /// is contained in the element Parameters collection.
        /// Workaround to replace ParameterSet.Contains.
        /// Why does this not work?
        /// return _parameter.Element.Parameters.Contains(_parameter);
        /// </summary>
        bool ContainedInCollectionUnnecessarilyComplicated(Parameter parameter, ParameterSet set)
        {
            bool same = false;

            foreach (Parameter setParameter in set)
            {
                if (IsSameParameter(parameter, setParameter))
                {
                    same = true;
                    break;
                }
            }

            return same;
        }

        /// <summary>
        /// Return true if the given element parameter 
        /// retrieved by get_Parameter( BuiltInParameter ) 
        /// is contained in the element Parameters collection.
        /// Workaround to replace ParameterSet.Contains.
        /// Why does the following statement not work?
        /// return _parameter.Element.Parameters.Contains(_parameter);
        /// </summary>
        bool ContainedInCollection(Parameter parameter, ParameterSet set)
        {
            return set.OfType<Parameter>().Any(x => x.Id == parameter.Id);
        }
        #endregion // Contained in ParameterSet Collection

        /// <summary>
        /// Revit external command to list all valid
        /// built-in parameters for a given selected
        /// element.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Select element
            var element = Util.GetSingleSelectedElementOrPrompt(uidoc);

            if (element == null)
            {
                return Result.Cancelled;
            }

            bool isSymbol = false;

            // For a family instance, ask user whether to
            // display instance or type parameters; in a
            // similar manner, we could add dedicated
            // switches for Wall --> WallType,
            // Floor --> FloorType etc. ...

            if (element is FamilyInstance)
            {
                var inst = element as FamilyInstance;

                if (inst.Symbol != null)
                {
                    var symbol_name = Util.ElementDescription(inst.Symbol, true);
                    var family_name = Util.ElementDescription(inst.Symbol.Family, true);
                    var msg = string.Format(_type_prompt, "is a family instance");

                    if (!Util.QuestionMsg(msg))
                    {
                        element = inst.Symbol;
                        isSymbol = true;
                    }
                }
            }
            else if (element.CanHaveTypeAssigned())
            {
                ElementId typeId = element.GetTypeId();

                if (typeId == null)
                {
                    Util.InfoMsg("Element can have a type, but the current type is null.");
                }
                else if (typeId == ElementId.InvalidElementId)
                {
                    Util.InfoMsg("Element can have a type, but the current type id is the invalid element id.");
                }
                else
                {
                    Element type = doc.GetElement(typeId);

                    if (type == null)
                    {
                        Util.InfoMsg("Element has a type, but it cannot be accessed.");
                    }
                    else
                    {
                        string msg = string.Format(_type_prompt, "has an element type");

                        if (!Util.QuestionMsg(msg))
                        {
                            element = type;
                            isSymbol = true;
                        }
                    }
                }
            }

            // Retrieve parameter data

            var data = new List<ParameterData>();

            var set = element.Parameters;
            bool containedInCollection;

            /* 
             * !!! This implemention does not work properly
             * if enum has the same integer value
             * For example, BuiltInParameter.All_MODEL_COST and
             * BuiltInParameter.DOOR_COST have -1001205 integer value
             * 
               var bips = Enum.GetValues(typeof(BuiltInParameter));
               int n = bips.Length;
             * 
             */

            var bipNames = Enum.GetNames(typeof(BuiltInParameter));

            foreach (var bipName in bipNames)
            {
                if (!Enum.TryParse(bipName, out BuiltInParameter bip))
                {
                    continue;
                }

                try
                {
                    var parameter = element.get_Parameter(bip);

                    if (parameter != null)
                    {
                        string valueString = (StorageType.ElementId == parameter.StorageType) ? Util.GetParameterValue2(parameter, doc) : parameter.AsValueString();

                        containedInCollection = ContainedInCollection(parameter, set);

                        data.Add(new ParameterData(bip, parameter, valueString, containedInCollection, bipName));
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("Exception retrieving built-in parameter {0}: {1}", bip, ex);
                }
            }

            // Retrieve parameters from Element.Parameters collection
            foreach (Parameter parameter in element.Parameters)
            {
                var valueString = (StorageType.ElementId == parameter.StorageType) ? Util.GetParameterValue2(parameter, doc) : parameter.AsValueString();
                var parameterData = new ParameterData((parameter.Definition as InternalDefinition).BuiltInParameter, parameter, valueString, true, null);

                if (!data.Contains(parameterData))
                {
                    data.Add(parameterData);
                }
            }

            // Display form
            var description = Util.ElementDescription(element, true) + (isSymbol ? " Type" : " Instance");

            var formViewModel = new BipCheckerListViewModel(element, description, data);
            var form = new BipCheckerListView { DataContext = formViewModel };

            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}
