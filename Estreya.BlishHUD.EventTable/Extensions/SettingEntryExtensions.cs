namespace Estreya.BlishHUD.EventTable.Extensions
{
    using Blish_HUD.Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class SettingEntryExtensions
    {
        
        public static float GetValue(this SettingEntry<float> settingEntry)
        {
            if (settingEntry == null) return 0;

            var range = GetRange(settingEntry);

            if (!range.HasValue) return settingEntry.Value;

            if (settingEntry.Value > range.Value.Max)
            {
                return range.Value.Max;
            }

            if (settingEntry.Value < range.Value.Min)
            {
                return range.Value.Min;
            }

            return settingEntry.Value;
        }

        public static int GetValue(this SettingEntry<int> settingEntry)
        {
            if (settingEntry == null) return 0;

            var range = GetRange(settingEntry);

            if (!range.HasValue) return settingEntry.Value;

            if (settingEntry.Value > range.Value.Max)
            {
                return range.Value.Max;
            }

            if (settingEntry.Value < range.Value.Min)
            {
                return range.Value.Min;
            }

            return settingEntry.Value;
        }

        public static (int Min, int Max)? GetRange(this SettingEntry<int> settingEntry)
        {
            if (settingEntry == null) return null;

            var crList = settingEntry.GetComplianceRequisite().Where(cr => cr is IntRangeRangeComplianceRequisite).ToList();

            if (crList.Count > 0)
            {
                IntRangeRangeComplianceRequisite intRangeCr = (IntRangeRangeComplianceRequisite)crList[0];
                return (intRangeCr.MinValue, intRangeCr.MaxValue);
            }

            return null;
        }

        public static (float Min, float Max)? GetRange(this SettingEntry<float> settingEntry)
        {
            if (settingEntry == null) return null;

            var crList = settingEntry.GetComplianceRequisite().Where(cr => cr is FloatRangeRangeComplianceRequisite).ToList();

            if (crList.Count > 0)
            {
                FloatRangeRangeComplianceRequisite floatRangeCr = (FloatRangeRangeComplianceRequisite)crList[0];
                return (floatRangeCr.MinValue, floatRangeCr.MaxValue);
            }

            return null;
        }

        public static bool IsDisabled(this SettingEntry settingEntry)
        {
            return settingEntry.GetComplianceRequisite()?.Any(cr => cr is SettingDisabledComplianceRequisite) ?? false;
        }
    }
}
