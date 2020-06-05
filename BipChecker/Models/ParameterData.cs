using Autodesk.Revit.DB;
using BipChecker.Utils;
using System.Diagnostics;

namespace BipChecker.Models
{
    /// <summary>
    /// A class used to manage the data of an element parameter.
    /// </summary>
    public class ParameterData
    {
        BuiltInParameter _enum;
        readonly Parameter _parameter;
        private readonly string _parameterName;

        string GetValue
        {
            get
            {
                string value;

                switch (_parameter.StorageType)
                {
                    // database value, internal units, e.g. feet:
                    case StorageType.Double:
                        value = Util.RealString(_parameter.AsDouble());
                        break;
                    case StorageType.Integer:
                        value = _parameter.AsInteger().ToString();
                        break;
                    case StorageType.String:
                        value = _parameter.AsString();
                        break;
                    case StorageType.ElementId:
                        value = _parameter.AsElementId().IntegerValue.ToString();
                        break;
                    case StorageType.None:
                        value = "None";
                        break;
                    default:
                        Debug.Assert(false, "unexpected storage type"); value = string.Empty;
                        break;
                }

                return value;
            }
        }

        public ParameterData(BuiltInParameter bip, Parameter parameter, string valueStringOrElementDescription, bool containedInCollection, string parameterName)
        {
            _enum = bip;
            _parameter = parameter;
            _parameterName = parameterName;

            ValueString = valueStringOrElementDescription;
            Value = GetValue;

            var parameterDefinition = _parameter.Definition;

            ParameterGroup = parameterDefinition.ParameterGroup.ToString();
            GroupName = LabelUtils.GetLabelFor(parameterDefinition.ParameterGroup);
            ContainedInCollection = containedInCollection ? "Y" : "N";
        }

        public string Enum
        {
            get
            {
                return _parameterName ?? _enum.ToString();
            }
        }

        public string Name
        {
            get { return _parameter.Definition.Name; }
        }

        public string Type
        {
            get
            {
                ParameterType pt = _parameter.Definition.ParameterType; // returns 'Invalid' for 'ElementId'
                string s = ParameterType.Invalid == pt ? "" : "/" + pt.ToString();
                return _parameter.StorageType.ToString() + s;
            }
        }

        public string ReadWrite
        {
            get { return _parameter.IsReadOnly ? "read-only" : "read-write"; }
        }

        /// <summary>
        /// Value string or element description
        /// in case of an element id.
        /// </summary>
        public string ValueString { get; set; }
        public string Value { get; set; }
        public string ParameterGroup { get; set; }
        public string GroupName { get; set; }

        /// <summary>
        /// Contained in the Element.Parameters collection?
        /// </summary>
        public string ContainedInCollection { get; set; }

        public string Shared
        {
            get { return _parameter.IsShared ? "Shared" : "Non-shared"; }
        }

        public string Guid
        {
            get { return _parameter.IsShared ? _parameter.GUID.ToString() : string.Empty; }
        }
    }
}
