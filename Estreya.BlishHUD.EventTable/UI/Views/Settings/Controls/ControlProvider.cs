namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class ControlProvider
    {
        private static readonly Logger Logger = Logger.GetLogger<ControlProvider>();
        protected static List<ControlProvider> Provider { get; } = new List<ControlProvider>()
        {
            new CheckboxProvider(),
            new TextBoxProvider(),
            new FloatTrackBarProvider(),
            new IntTrackBarProvider(),
            new KeybindingProvider()
        };

        protected static void Register<T>(ControlProvider<T> controlProvider)
        {
            lock (Provider)
            {
                if (Provider.Any(p =>
                {
                    return p is ControlProvider<T> provider && provider.Type == controlProvider.Type;
                }))
                {
                    throw new ArgumentException($"Control Type \"{controlProvider.Type}\" already registered.");
                }

                Provider.Add(controlProvider);
            }
        }


        public static Control Create<T>(SettingEntry<T> settingEntry, Func<SettingEntry<T>, T, bool> validationFunction, int width, int heigth, int x, int y)
        {
            List<ControlProvider> providers = Provider.Where(p =>
            {
                return p is ControlProvider<T> provider && provider.Type == settingEntry.SettingType;
            }).ToList();

            if (providers.Count == 0)
            {
                if (settingEntry?.SettingType.IsEnum ?? false)
                {
                    Register((ControlProvider<T>)Activator.CreateInstance(typeof(EnumProvider<>).MakeGenericType(typeof(T))));
                    return Create(settingEntry, validationFunction, width, heigth, x, y);
                }
                else
                {
                    throw new NotSupportedException($"Control Type \"{settingEntry.SettingType}\" is not supported.");
                }
            }

            return (providers.First() as ControlProvider<T>).CreateControl(settingEntry, validationFunction, width, heigth, x, y);
        }

        public static Control Create<T>(int width, int heigth, int x, int y)
        {
            List<ControlProvider> providers = Provider.Where(p =>
            {
                return p is ControlProvider<T> provider && provider.Type == typeof(T);
            }).ToList();

            if (providers.Count == 0)
            {
                if (typeof(T).IsEnum)
                {
                    Register((ControlProvider<T>)Activator.CreateInstance(typeof(EnumProvider<>).MakeGenericType(typeof(T))));
                    return Create<T>(width, heigth, x, y);
                }
                else
                {
                    throw new NotSupportedException($"Control Type \"{typeof(T)}\" is not supported.");
                }
            }

            return (providers.First() as ControlProvider<T>).CreateControl(null,null, width, heigth, x, y);
        }
    }
}
