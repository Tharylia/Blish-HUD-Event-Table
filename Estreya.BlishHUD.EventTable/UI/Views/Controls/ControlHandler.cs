namespace Estreya.BlishHUD.EventTable.UI.Views.Controls;

using Blish_HUD.Controls;
using Blish_HUD.Settings;
using Estreya.BlishHUD.EventTable.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public class ControlHandler
{
    private static List<ControlProvider> Provider { get; } = new List<ControlProvider>()
    {
        new TextBoxProvider(),
        new IntTrackBarProvider(),
        new IntTextBoxProvider(),
        new FloatTrackBarProvider(),
        new CheckboxProvider(),
        new TimeSpanProvider(),
        new KeybindingProvider()
    };

    private static void Register<T, TOverrideType>(ControlProvider<T, TOverrideType> controlProvider)
    {
        lock (Provider)
        {
            if (Provider.Any(p =>
            {
                return p is ControlProvider<T,TOverrideType> provider && provider.Types.Intersect(controlProvider.Types).Any();
            }))
            {
                throw new ArgumentException($"Control Types \"{controlProvider.Types}\" already registered.");
            }

            Provider.Add(controlProvider);
        }
    }

    public static Control CreateFromSetting<T>(SettingEntry<T> settingEntry, Func<SettingEntry<T>, T, bool> validationFunction, int width, int heigth, int x, int y)
    {
        List<ControlProvider> providers = Provider.Where(p =>
        {
            return p is ControlProvider<T,T> provider && provider.Types.Contains(settingEntry.SettingType);
        }).ToList();

        if (providers.Count == 0)
        {
            if (settingEntry?.SettingType.IsEnum ?? false)
            {
                Register((ControlProvider<T,T>)Activator.CreateInstance(typeof(EnumProvider<>).MakeGenericType(typeof(T))));
                return CreateFromSetting(settingEntry, validationFunction, width, heigth, x, y);
            }
            else
            {
                throw new NotSupportedException($"Control Type \"{settingEntry.SettingType}\" is not supported.");
            }
        }

        var provider = providers.First();

        return (provider as ControlProvider<T,T>).CreateControl(new BoxedValue<T>(settingEntry.Value, val => settingEntry.Value = val), (obj) => !settingEntry.IsDisabled(), (T val) =>
        {
            return validationFunction?.Invoke(settingEntry, val) ?? true;
        }, settingEntry.GetRange(), width, heigth, x, y);
    }

    public static Control CreateFromProperty<TObject, TProperty>(TObject obj, Expression<Func<TObject, TProperty>> expression, Func<TObject, bool> isEnabled, Func<TProperty, bool> validationFunction, int width, int heigth, int x, int y)
    {
        List<ControlProvider> providers = Provider.Where(p =>
        {
            return p is ControlProvider<TProperty, TProperty> provider && provider.Types.Contains(typeof(TProperty));
        }).ToList();

        if (providers.Count == 0)
        {
            if (typeof(TProperty).IsEnum)
            {
                Register((ControlProvider<TProperty,TProperty>)Activator.CreateInstance(typeof(EnumProvider<>).MakeGenericType(typeof(TProperty))));
                return CreateFromProperty(obj, expression, isEnabled, validationFunction, width, heigth, x, y);
            }
            else
            {
                throw new NotSupportedException($"Control Type \"{typeof(TProperty)}\" is not supported.");
            }
        }

        var provider = providers.First();

        return (provider as ControlProvider<TProperty,TProperty>).CreateControl(new BoxedValue<TProperty>(expression.Compile().Invoke(obj), val =>
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                var property = memberExpression.Member as PropertyInfo;
                if (property != null)
                {
                    property.SetValue(obj, val, null);
                }
            }
        }),
        (val) => isEnabled?.Invoke(obj) ?? true, validationFunction, null, width, heigth, x, y);
    }

    public static Control CreateFromPropertyWithChangedTypeValidation<TObject, TProperty, TOverrideType>(TObject obj, Expression<Func<TObject, TProperty>> expression, Func<TObject, bool> isEnabled, Func<TOverrideType, bool> validationFunction, int width, int heigth, int x, int y)
    {
        List<ControlProvider> providers = Provider.Where(p =>
        {
            return p is ControlProvider<TProperty,TOverrideType> provider && provider.Types.Contains(typeof(TProperty));
        }).ToList();

        if (providers.Count == 0)
        {
            if (typeof(TProperty).IsEnum)
            {
                Register((ControlProvider<TProperty, TOverrideType>)Activator.CreateInstance(typeof(EnumProvider<>).MakeGenericType(typeof(TProperty))));
                return CreateFromPropertyWithChangedTypeValidation(obj, expression, isEnabled, validationFunction, width, heigth, x, y);
            }
            else
            {
                throw new NotSupportedException($"Control Type \"{typeof(TProperty)}\" is not supported.");
            }
        }

        var provider = providers.First();

        return (provider as ControlProvider<TProperty, TOverrideType>).CreateControl(new BoxedValue<TProperty>(expression.Compile().Invoke(obj), val =>
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                var property = memberExpression.Member as PropertyInfo;
                if (property != null)
                {
                    property.SetValue(obj, val, null);
                }
            }
        }),
        (val) => isEnabled?.Invoke(obj) ?? true, validationFunction, null, width, heigth, x, y);
    }

    public static Control Create<T>(int width, int heigth, int x, int y)
    {
        List<ControlProvider> providers = Provider.Where(p =>
        {
            return p is ControlProvider<T,T> provider && provider.Types.Contains(typeof(T));
        }).ToList();

        if (providers.Count == 0)
        {
            if (typeof(T).IsEnum)
            {
                Register((ControlProvider<T, T>)Activator.CreateInstance(typeof(EnumProvider<>).MakeGenericType(typeof(T))));
                return Create<T>(width, heigth, x, y);
            }
            else
            {
                throw new NotSupportedException($"Control Type \"{typeof(T)}\" is not supported.");
            }
        }

        return (providers.First() as ControlProvider<T, T>).CreateControl(null, null, null, null, width, heigth, x, y);
    }
}
