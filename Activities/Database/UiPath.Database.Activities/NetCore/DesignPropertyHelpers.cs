using System.Activities.DesignViewModels;
using System.Threading.Tasks;

namespace UiPath.Database.Activities.NetCore.ViewModels.Helpers
{
    internal static class DesignPropertyHelpers
    {
        /// <summary>
        /// Toggles visibility between two <see cref="DesignProperty"/> variables and sets the hidden one's value to null.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="propertyToShow"></param>
        /// <param name="propertyToHide"></param>
        /// <returns></returns>
        public static Task ToggleDesignProperties<T1, T2>(T1 propertyToShow, T2 propertyToHide)
            where T1 : DesignProperty
            where T2 : DesignProperty
        {
            propertyToShow.IsVisible = true;
            propertyToShow.IsRequired = true;

            propertyToHide.IsVisible = false;
            propertyToHide.IsRequired = false;
            propertyToHide.Value = null;

            return Task.CompletedTask;
        }
    }
}
