using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace OutOfPhaseTraceScheduleAnalyzer
{
    public partial class Level2DetailView : UserControl
    {
        private Top top;
        private Epoch epoch;

        public Level2DetailView()
        {
            InitializeComponent();
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Top Top { get { return top; } set { top = value; } }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Epoch Epoch
        {
            get
            {
                return epoch;
            }
            set
            {
                epoch = value;
                RecalcEpochChanged();
            }
        }

        private void RecalcEpochChanged()
        {
            StringBuilder text = new StringBuilder();

            if (epoch != null)
            {
                bool denormals = false;
                for (int i = 0; i < epoch.Data.Length; i++)
                {
                    if (epoch.Data[i].Denormals != 0)
                    {
                        denormals = true;
                        break;
                    }
                }
                if (denormals)
                {
                    text.AppendLine("Denormals:");
                    for (int i = 0; i < epoch.Data.Length; i++)
                    {
                        if (epoch.Data[i].Denormals != 0)
                        {
                            text.AppendLine(String.Format("    {0,-3} ({1,-2} {2})", epoch.Data[i].Denormals, i, top.Definitions[i].Name));
                        }
                    }
                }
            }

            textBox.Text = text.ToString();
            textBox.SelectionLength = 0;
            textBox.SelectionStart = 0;
        }
    }
}
