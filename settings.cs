using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace _18practical
{
    public partial class settings : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        
        private int SizeItem = 0;
        private int formWidth = 0;

        private readonly string dataDirectory = Path.Combine(Application.StartupPath, "data");

        public settings()
        {
            InitializeComponent();
            panel1.MouseDown += new MouseEventHandler(panel1_MouseDown);
            panel1.MouseMove += new MouseEventHandler(panel1_MouseMove);
            panel1.MouseUp += new MouseEventHandler(panel1_MouseUp);

            cbSizeItem.Items.AddRange(new string[] { "Минимальный", "Средний", "Максимальный" });
            cbformWidth.Items.AddRange(new string[] { "Минимальный", "Средний", "Максимальный" });

            ApplyCustomStylingToAllComboBoxes(this);

            LoadSettingsFromFile();
            AssignComboBoxValues();
        }
        void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }
        void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }
        void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }
        private void label1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cbSizeItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.ComboBox comboBox = (System.Windows.Forms.ComboBox)sender;

            switch (comboBox.SelectedItem.ToString())
            {
                case "Минимальный":
                    SizeItem = 100;
                    break;
                case "Средний":
                    SizeItem = 150;
                    break;
                case "Максимальный":
                    SizeItem = 200;
                    break;
            }
        }
        private void cbformWidth_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.ComboBox comboBox = (System.Windows.Forms.ComboBox)sender;

            switch (comboBox.SelectedItem.ToString())
            {
                case "Минимальный":
                    formWidth = 680;
                    break;
                case "Средний":
                    formWidth = 880;
                    break;
                case "Максимальный":
                    formWidth = 1000;
                    break;
            }
        }

        private void ApplyCustomComboBoxStyling(System.Windows.Forms.ComboBox comboBox)
        {
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;

            comboBox.DrawItem += (sender, e) =>
            {
                if (e.Index < 0)
                {
                    return;
                }

                System.Windows.Forms.ComboBox cb = (System.Windows.Forms.ComboBox)sender;
                string itemText = cb.Items[e.Index].ToString();
                SolidBrush fontBrush = new SolidBrush(Color.White);
                SolidBrush backgroundBrush = new SolidBrush(ColorTranslator.FromHtml("#303030"));

                e.DrawBackground();
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
                e.Graphics.DrawString(itemText, e.Font, fontBrush, e.Bounds, StringFormat.GenericDefault);

                e.DrawFocusRectangle();
            };

            comboBox.BackColor = ColorTranslator.FromHtml("#303030");
            comboBox.ForeColor = Color.White;

            comboBox.DropDown += (sender, e) =>
            {
                System.Windows.Forms.ComboBox cb = (System.Windows.Forms.ComboBox)sender;
                cb.BackColor = ColorTranslator.FromHtml("#303030");
                cb.ForeColor = Color.White;
            };

            comboBox.DropDownClosed += (sender, e) =>
            {
                System.Windows.Forms.ComboBox cb = (System.Windows.Forms.ComboBox)sender;
                cb.BackColor = ColorTranslator.FromHtml("#303030");
                cb.ForeColor = Color.White;
            };
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }
        private void ApplyCustomStylingToAllComboBoxes(Control control)
        {
            foreach (Control childControl in control.Controls)
            {
                if (childControl is System.Windows.Forms.ComboBox)
                {
                    ApplyCustomComboBoxStyling((System.Windows.Forms.ComboBox)childControl);
                }

                if (childControl.HasChildren)
                {
                    ApplyCustomStylingToAllComboBoxes(childControl);
                }
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string filePath = Path.Combine(dataDirectory, "style.txt");
            string data = $"SizeItem = {SizeItem}\nformWidth = {formWidth}";
            File.WriteAllText(filePath, data);
            this.Close();
        }

        private void LoadSettingsFromFile()
        {
            string filePath = Path.Combine(dataDirectory, "style.txt");

            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    if (line.StartsWith("SizeItem"))
                    {
                        SizeItem = int.Parse(line.Split('=')[1].Trim());
                    }
                    else if (line.StartsWith("formWidth"))
                    {
                        formWidth = int.Parse(line.Split('=')[1].Trim());
                    }
                }
            }
        }

        private void AssignComboBoxValues()
        {
            if (SizeItem == 100)
            {
                cbSizeItem.SelectedIndex = 0;
            }
            else if (SizeItem == 150)
            {
                cbSizeItem.SelectedIndex = 1;
            }
            else if (SizeItem == 200)
            {
                cbSizeItem.SelectedIndex = 2;
            }
            else
            {
                cbSizeItem.SelectedIndex = 0;
            }

            if (formWidth == 680)
            {
                cbformWidth.SelectedIndex = 0;
            }
            else if (formWidth == 880)
            {
                cbformWidth.SelectedIndex = 1;
            }
            else if (formWidth == 1000)
            {
                cbformWidth.SelectedIndex = 2;
            }
            else
            {
                cbformWidth.SelectedIndex = 0;
            }
        }
    }
}
