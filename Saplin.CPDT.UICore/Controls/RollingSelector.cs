using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Saplin.CPDT.UICore.Controls
{
    public class KeyValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class RollingSelector : ExtendedLabel
    {
        private bool clickAssigned = false;

        public static readonly BindableProperty ItemsProperty =
            BindableProperty.Create(
                propertyName: nameof(Items),
                returnType: typeof(KeyValue[]),
                declaringType: typeof(ExtendedLabel),
                defaultValue: null,
                defaultBindingMode: BindingMode.OneWay,
                propertyChanged: ItemsPropertyChanged
             );

        private int count = 0;
        private int selectedIndex = 0;

        public int Count
        {
            get { return count;}
        }

        private void UpdateSelectedIndex()
        {
            selectedIndex = 0;

            var selectedKey = !string.IsNullOrEmpty(SelectedItemKey) ? SelectedItemKey.ToLower() : string.Empty;

            if (Items == null) return;
            foreach (var kv in Items)
            {
                if (string.Equals(kv.Key, selectedKey, StringComparison.OrdinalIgnoreCase)) break;
                selectedIndex++;
            }
        }

        private static void ItemsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as RollingSelector;

            if (newValue == null) control.count = 0; else control.count = (newValue as KeyValue[]).Length;
            if (control.count == 0)
            {
                control.Text = string.Empty;
                return;
            }

            var selectedKey = !string.IsNullOrEmpty(control.SelectedItemKey) ? control.SelectedItemKey.ToLower() : string.Empty;

            if (!string.IsNullOrEmpty(selectedKey))
            {
                if (!control.Items.Any(kv => string.Equals(kv.Key, selectedKey, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Can't select key which is not presetn as key in Items collection");

                control.Text = ((IEnumerable<KeyValue>)control.Items)
                    .Where(kv => string.Equals(kv.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
                    .Select<KeyValue, string>(kv => kv.Value as string)
                    .Single();

                control.UpdateSelectedIndex();
            }

            if (!control.clickAssigned)
            {
                control.clickAssigned = true;
                control.Clicked += (s, e) => control.Step(1);
            }
        }

        // Android TV: move the focused setting with the left/right remote keys.
        // The existing tap interaction continues to call Step(1).
        public void Step(int direction)
        {
            var values = Items == null ? null : Items.ToArray();
            if (values == null || values.Length == 0 || direction == 0) return;

            UpdateSelectedIndex();
            if (selectedIndex >= values.Length) selectedIndex = 0;
            selectedIndex = (selectedIndex + (direction < 0 ? -1 : 1) + values.Length) % values.Length;
            Text = values[selectedIndex].Value;
            SelectedItemKey = values[selectedIndex].Key;
        }

        public IEnumerable<KeyValue> Items
        {
            get { return GetValue(ItemsProperty) as IEnumerable<KeyValue>; }
            set
            {
                SetValue(ItemsProperty, value);
            }
        }

        public static readonly BindableProperty SelectedItemKeyProperty =
            BindableProperty.Create(
                propertyName: nameof(SelectedItemKey),
                returnType: typeof(string),
                declaringType: typeof(ExtendedLabel),
                defaultValue: null,
                defaultBindingMode: BindingMode.TwoWay,
                propertyChanged: SelectedItemKeyChanged
             );

        public string SelectedItemKey
        {
            get { return GetValue(SelectedItemKeyProperty) as string; }
            set
            {
                SetValue(SelectedItemKeyProperty, value);
            }
        }

        private static void SelectedItemKeyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as RollingSelector;

            if (control.Items == null) return;

            var value = newValue is string ? (newValue as string).ToLower() : string.Empty;

            //TODO check unique keys
            if (!control.Items.Any(kv => string.Equals(kv.Key, value, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Can't select key which is not presetn as key in Items collection");

            if (string.IsNullOrEmpty(value))
            {
                control.Text = "";
            }

            control.Text = ((IEnumerable<KeyValue>)control.Items)
                .Where(kv => string.Equals(kv.Key, value, StringComparison.OrdinalIgnoreCase))
                .Select<KeyValue, string>(kv => kv.Value as string)
                .Single();

            control.UpdateSelectedIndex();
        }
    }
}
