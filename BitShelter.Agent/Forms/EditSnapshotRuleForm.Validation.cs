using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BitShelter.Agent.Forms
{
  public partial class EditSnapshotRuleForm
  {
    //
    // Validation

    private void ValidateSnapshot()
    {
      Valid = RuleName == tbName.Text || ValidateName();
      Valid = ValidateSchedule() && Valid;
      Valid = ValidateBackup() && Valid;
    }

    private bool ValidateName()
    {
      bool valid = !string.IsNullOrWhiteSpace(tbName.Text);

      if (valid)
        SetStyleValid(lblName);

      else
        SetStyleInvalid(lblName);

      return valid;
    }

    private bool ValidateGeneric(Func<bool> validateFunc, Control clueControl)
    {
      bool valid = validateFunc();

      if (valid)
        SetStyleValid(clueControl);

      else
        SetStyleInvalid(clueControl);

      return valid;
    }

    private void SetStyleValid(params Control[] controlArray)
    {
      if (controlArray.Count() == 0)
        return;

      Font regularFont = new Font(controlArray[0].Font, FontStyle.Regular);

      foreach (Control lbl in controlArray)
      {
        if (lbl.Font.Bold == true)
        {
          lbl.Font = regularFont;
          lbl.ForeColor = Color.Black;
        }
      }
    }

    private void SetStyleInvalid(params Control[] controlArray)
    {
      if (controlArray.Count() == 0)
        return;

      Font boldFont = new Font(controlArray[0].Font, FontStyle.Bold);

      foreach (Control lbl in controlArray)
      {
        if (lbl.Font.Bold == false)
        {
          lbl.Font = boldFont;
          lbl.ForeColor = Color.Red;
        }
      }
    }
  }
}
