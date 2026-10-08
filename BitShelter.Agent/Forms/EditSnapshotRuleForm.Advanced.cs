using BitShelter.Agent.Controls;
using BitShelter.Models;
using BitShelter.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitShelter.Agent.Forms
{
  partial class EditSnapshotRuleForm
  {
    public void InitBaseAdvanced()
    {
      cbPruningStrategy.FormattingEnabled = true;
      // Attach Format before filling: adding a Format handler refreshes the items and resets the selection.
      cbPruningStrategy.Format += (_, e) => e.Value = (PruningStrategy)e.ListItem == PruningStrategy.Global
        ? "Global: also keep room under the volume limit"
        : "Local: by lifetime only";
      cbPruningStrategy.FillWithEnum(PruningStrategy.Global);
    }

    public void InitEditAdvanced(SnapshotRule rule)
    {
      cbUseVssWriters.Checked = rule.UsesVssWriters;
      nbSnapFailRetryCount.Value = rule.MaxRetryCount;
      cbSnapFailRestartVSS.Checked = rule.RetryRestartVSSService;
      cbPruningStrategy.SelectedIndex = cbPruningStrategy.Items.IndexOf(rule.PruningStrategy);
    }



    //
    // Misc

    private void FillAdvanced(SnapshotRule rule)
    {
      rule.VssContext = cbUseVssWriters.Checked ? VssSnapshotContextInternal.ClientAccessibleWriters : VssSnapshotContextInternal.ClientAccessible;
      rule.MaxRetryCount = (int)nbSnapFailRetryCount.Value;
      rule.RetryRestartVSSService = cbSnapFailRestartVSS.Checked;
      rule.PruningStrategy = (PruningStrategy)cbPruningStrategy.SelectedItem;
    }



    //
    // Validation

    public void ValidateAdvanced()
    {
      //Valid = ValidateGeneric(() => )
    }
  }
}
