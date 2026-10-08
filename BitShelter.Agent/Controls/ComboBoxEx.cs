using System;
using System.Windows.Forms;

namespace BitShelter.Agent.Controls
{
  public static class ComboBoxEx
  {
    public static void FillWithEnum<T>(this ComboBox cb, T defaultValue)
    {
      cb.DataSource = Enum.GetValues(typeof(T));
      cb.SelectedIndex = cb.Items.IndexOf(defaultValue);
    }
  }
}
