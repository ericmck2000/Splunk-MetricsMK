﻿using BrightIdeasSoftware;
using Humanizer;
using InputsBuilder.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
namespace InputsBuilder.Controls
{
    public partial class PerfCounterTreeView : TreeListView
    {

        public PerfCounterTreeView(BindingList<Splunk_Index> Indexes)
        {
            #region Add Columns
            this.Columns.AddRange(new ColumnHeader[] {
                new BrightIdeasSoftware.OLVColumn
                {
                    Text = "Category",
                    Name = "Repo",                    
                    //CheckBoxes = true,
                    AspectGetter = delegate(object x)
                    {
                        if (x is SelectedCategory cat)
                            return  cat.Name;
                        if(x is SelectedCounter count)
                            return count.Name;
                        return null;
                    },
                    MinimumWidth = 30 * 12,
                    Sortable = true,
                },
                new OLVColumn
                {
                    Text = "Instances",
                    Name = "Instances",
                    Width = 100,
                    AspectGetter = delegate(object x)
                    {
                        if (x is SelectedCategory cat)
                            return cat.Category.GetInstanceNames().Length;
                        //if(x is SelectedCounter count)
                        //    return count.InstanceCount;
                        return null;
                    },
                },
                new OLVColumn
                {
                    Text = "Interval",
                    Name = "Interval",
                    Width = 100,
                    AspectGetter = delegate(object x)
                    {
                        if (x is SelectedCategory cat)
                            return cat.CollectionIntervalSeconds.Seconds().Humanize();
                        return null;
                    },
                },
                new OLVColumn
                {
                    Text = "Index",
                    Name = "Index",
                    Width = 100,
                    IsEditable = true,
                    AutoCompleteEditorMode = AutoCompleteMode.SuggestAppend,
                    AspectGetter = delegate(object x)
                    {
                        if(x is SelectedCategory sel)
                            return sel.Index?.Name;
                        return null;
                    },
                },
                new OLVColumn
                {
                    Text = "Retention (Days)",
                    Name = "RetentionDays",
                    Width = 100,
                    AspectGetter = delegate(object x)
                    {
                        if(x is SelectedCategory sel)
                            return sel.Index?.RetentionDays?.Days().Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Day) ?? "Not Set";
                        return null;
                    },
                },
                new OLVColumn
                {
                    Text = "Retention (MB)",
                    Name = "RetentionSize",
                    Width = 100,
                    AspectGetter = delegate(object x)
                    {
                        if(x is SelectedCategory sel)
                            return sel.Index?.MaxDataSizeMB?.Megabytes().Humanize() ?? "Not Set";
                        return null;
                    },
                },
                new OLVColumn
                {
                    Text = "Partial Metric Name",
                    Name = "MetricName",
                    Width = 100,
                    AspectGetter = delegate(object x)
                    {
                        if (x is SelectedCategory cat)
                            return cat.Metric_Prefix;
                        if(x is SelectedCounter count)
                            return count.MetricName;
                        return null;
                    },
                },
                new OLVColumn
                {
                    Text = "Metric Name",
                    Name = "MetricNameFull",
                    Width = 200,
                    AspectGetter = delegate(object x)
                    {
                        if(x is SelectedCounter count)
                            return string.Join('.', count.Category.Metric_Prefix, count.MetricName);
                        return null;
                    },
                }
            });
            #endregion
            #region Expand / Children
            this.CanExpandGetter = delegate (object x)
            {
                if (x is SelectedCategory sel && sel.Category.GetInstanceNames().Length > 0)
                    return true;

                return false;
            };
            this.ChildrenGetter = delegate (object x)
            {
                if (x is SelectedCategory sel)
                {
                    sel.Expand();
                    return sel.Counters;
                }

                return null;
            };
            #endregion
            #region Editing
            CellEditStarting += delegate (object sender, CellEditEventArgs e)
            {
                switch (e.RowObject)
                {
                    case SelectedCategory cat:
                        switch (e.Column.Name)
                        {
                            case "Interval":
                                e.Control = new System.Windows.Forms.NumericUpDown()
                                {
                                    Bounds = e.CellBounds,
                                    Minimum = 1,
                                    DecimalPlaces = 0,
                                    Maximum = int.MaxValue,
                                    Value = cat.CollectionIntervalSeconds,
                                };
                                break;
                            case "RetentionDays":
                                e.Control = new System.Windows.Forms.NumericUpDown()
                                {
                                    Bounds = e.CellBounds,
                                    Minimum = 0,
                                    DecimalPlaces = 0,
                                    Maximum = int.MaxValue,
                                    Value = cat.Index.RetentionDays ?? 0,
                                };
                                break;
                            case "RetentionSize":
                                e.Control = new System.Windows.Forms.NumericUpDown()
                                {
                                    Bounds = e.CellBounds,
                                    Minimum = 0,
                                    DecimalPlaces = 0,
                                    Maximum = int.MaxValue,
                                    Value = cat.Index.RetentionDays ?? 0,
                                };
                                break;
                            case "Index":
                                {
                                    var acsc = new AutoCompleteStringCollection();
                                    acsc.AddRange(Indexes.Select(o => o.Name).ToArray());
                                    e.Control = new TextBox()
                                    {
                                        Bounds = e.CellBounds,
                                        AutoCompleteCustomSource = acsc,
                                        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                                        AutoCompleteSource = AutoCompleteSource.CustomSource,
                                        AcceptsTab = false,
                                        TextAlign = HorizontalAlignment.Left,
                                        Multiline = false,
                                        Text = cat.Index.Name,
                                        AcceptsReturn = true
                                    };
                                }
                                break;
                            case "MetricName":
                                {
                                    var acsc = new AutoCompleteStringCollection();
                                    var choices = this.Objects.OfType<SelectedCategory>().Select(o => o.Metric_Prefix).Distinct().Where(o => o != null).ToArray();
                                    acsc.AddRange(choices);
                                    e.Control = new TextBox()
                                    {
                                        Bounds = e.CellBounds,
                                        AutoCompleteCustomSource = acsc,
                                        AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                                        AutoCompleteSource = AutoCompleteSource.CustomSource,
                                        AcceptsTab = false,
                                        TextAlign = HorizontalAlignment.Left,
                                        Multiline = false,
                                        Text = cat.Metric_Prefix,
                                        AcceptsReturn = true
                                    };
                                }
                                break;
                            default:
                                e.Cancel = true;
                                return;
                        }

                        break;
                    case SelectedCounter count:
                        switch (e.Column.Name)
                        {
                            case "MetricName":
                                {
                                    //var acsc = new AutoCompleteStringCollection();
                                    //var choices = this.Categories.Select(o => o.Metric_Prefix).Distinct().Where(o => o != null).ToArray();
                                    //acsc.AddRange(choices);
                                    e.Control = new TextBox()
                                    {
                                        Bounds = e.CellBounds,
                                        //AutoCompleteCustomSource = acsc,
                                        //AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                                        //AutoCompleteSource = AutoCompleteSource.CustomSource,
                                        AcceptsTab = false,
                                        TextAlign = HorizontalAlignment.Left,
                                        Multiline = false,
                                        Text = count.MetricName,
                                        AcceptsReturn = true
                                    };
                                }
                                break;
                            case "MetricNameFull":
                                {
                                    //var acsc = new AutoCompleteStringCollection();
                                    //var choices = this.Categories.SelectMany(o => o.SelectedCounters.Select(Z => Z.MetricName)).Distinct().Where(o => o != null).ToArray();
                                    //acsc.AddRange(choices);
                                    e.Control = new TextBox()
                                    {
                                        Bounds = e.CellBounds,
                                        //AutoCompleteCustomSource = acsc,
                                        //AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                                        //AutoCompleteSource = AutoCompleteSource.CustomSource,
                                        AcceptsTab = false,
                                        TextAlign = HorizontalAlignment.Left,
                                        Multiline = false,
                                        Text = count.MetricName,
                                        AcceptsReturn = true
                                    };
                                }
                                break;
                            default:
                                e.Cancel = true;
                                return;
                        }
                        break;
                    default:
                        e.Cancel = true;
                        return;
                }
            };
            CellEditFinished += delegate (object sender, CellEditEventArgs e)
            {
                switch (e.RowObject)
                {
                    case SelectedCategory cat:
                        switch (e.Column.Name)
                        {
                            case "Interval":
                                cat.CollectionIntervalSeconds = (int)((NumericUpDown)e.Control).Value;
                                return;
                            case "RetentionDays":
                                {
                                    //The control cannot possible return a null value....
                                    //But, this does keep the compiler happy for the following line.
                                    var value = (int?)((NumericUpDown)e.Control).Value;
                                    cat.Index.RetentionDays = value == 0 ? null : value;
                                }
                                return;
                            case "RetentionSize":
                                {
                                    //The control cannot possible return a null value....
                                    //But, this does keep the compiler happy for the following line.
                                    var value = (int?)((NumericUpDown)e.Control).Value;
                                    cat.Index.MaxDataSizeMB = value == 0 ? null : value;
                                }
                                return;
                            case "Index":
                                var ctrl = (TextBox)e.Control;
                                if (!Indexes.Any(o => o.Name == (string)e.NewValue))
                                    Indexes.Add(new Splunk_Index { Name = (string)e.NewValue, RetentionDays = 90 });

                                cat.Index = Indexes.FirstOrDefault(o => o.Name == (string)e.NewValue);
                                return;
                            case "MetricName":
                                cat.Metric_Prefix = (string)e.NewValue;
                                return;
                            default:
                                e.Cancel = true;
                                return;
                        }
                    case SelectedCounter count:
                        switch (e.Column.Name)
                        {
                            case "MetricName":
                                count.MetricName = (string)e.NewValue;
                                return;

                            default:
                                e.Cancel = true;
                                return;
                        }
                    default:
                        e.Cancel = true;
                        return;
                }
            };
            #endregion
            #region Checkboxes
            this.BooleanCheckStateGetter = delegate (object x)
            {
                if (x is SelectedCategory sel)
                    //If counters == 0, means this category has not yet been expanded.... instead of checking it- expand it.
                    return sel.Checked;
                else if (x is SelectedCounter cnt)
                    return cnt.Checked;
                else
                    return false;
            };

            this.BooleanCheckStatePutter = delegate (object x, bool value)
            {
                if (x is SelectedCategory sel)
                    sel.Checked = value;
                else if (x is SelectedCounter cnt)
                    cnt.Checked = value;
                return value;
            };
            #endregion
        }
    }
}
