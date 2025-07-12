using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static _18practical.Form1;

namespace _18practical
{
    public partial class EditingTags : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private readonly string tagsFilePath;
        private List<Tag> tags;

        private SqlConnection sqlDB;
        private string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";
        public EditingTags()
        {
            InitializeComponent();
            panel1.MouseDown += new MouseEventHandler(panel1_MouseDown);
            panel1.MouseMove += new MouseEventHandler(panel1_MouseMove);
            panel1.MouseUp += new MouseEventHandler(panel1_MouseUp);

            tags = ReadTagsFromFile();
            InitializeInterface();
        }

        private void ConnectToSqlDb()
        {
            sqlDB = new SqlConnection(connectionString);
            sqlDB.Open();
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
        private List<Tag> ReadTagsFromFile()
        {
            List<Tag> tags = new List<Tag>();
            string query = @"SELECT t.tag_id, t.name, t.color, 
                     STUFF((SELECT ', ' + CAST(c.item_id AS VARCHAR) 
                            FROM dbo.Table_connection_item_tag c 
                            WHERE c.tag_id = t.tag_id 
                            FOR XML PATH('')), 1, 2, '') AS item_ids 
                     FROM dbo.Table_Tags t 
                     GROUP BY t.tag_id, t.name, t.color;";

            string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";
            using (SqlConnection sqlDB = new SqlConnection(connectionString))
            {
                sqlDB.Open();

                using (SqlCommand command = new SqlCommand(query, sqlDB))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            string colorString = reader.GetString(2);
                            Color color = ColorTranslator.FromHtml(colorString);
                            string itemIdsString = reader.GetString(3);

                            List<int> itemIds = itemIdsString
                                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse)
                                .ToList();

                            tags.Add(new Tag(id, name, color, itemIds));
                        }
                    }
                }
            }
            return tags;
        }

        private void InitializeInterface()
        {
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;
            flowLayoutPanel1.AutoScroll = true;

            foreach (var tag in tags)
            {
                Panel tagPanel = new Panel
                {
                    Size = new Size(300, 60),
                    BorderStyle = BorderStyle.FixedSingle // используем стандартную стиль границы для возможности её видимого рисования
                };
                // Цвет рамки
                tagPanel.Paint += (sender, e) => {
                    using (Pen borderPen = new Pen(ColorTranslator.FromHtml("#32274F")))
                    {
                        e.Graphics.DrawRectangle(borderPen, 0, 0, tagPanel.Width - 1, tagPanel.Height - 1);
                    }
                };

                TextBox nameTextBox = new TextBox
                {
                    Text = tag.Name,
                    Location = new Point(5, 10),
                    Width = tagPanel.Width / 2 - 10,
                    BackColor = ColorTranslator.FromHtml("#303030"),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.None
                };

                Panel colorPanel = new Panel
                {
                    BackColor = tag.Color,
                    Location = new Point(160, 15),
                    Size = new Size(20, 20),
                    Cursor = Cursors.Hand
                };

                Label deleteLabel = new Label
                {
                    Text = "Удалить",
                    Location = new Point(190, 15),
                    Cursor = Cursors.Hand,
                    ForeColor = Color.White
                };

                Label itemCountLabel = new Label
                {
                    Text = $"Количество элементов: {tag.ItemIds.Count}",
                    Location = new Point(5, 35),
                    AutoSize = true,
                    ForeColor = Color.White
                };

                tagPanel.Controls.Add(nameTextBox);
                tagPanel.Controls.Add(colorPanel);
                tagPanel.Controls.Add(deleteLabel);
                tagPanel.Controls.Add(itemCountLabel);

                flowLayoutPanel1.Controls.Add(tagPanel);

                deleteLabel.Click += (sender, args) => DeleteTag(tag.Id, tagPanel);

                colorPanel.Click += (sender, args) =>
                {
                    int scrollPosition = flowLayoutPanel1.VerticalScroll.Value;

                    ColorDialog colorDialog = new ColorDialog();
                    if (colorDialog.ShowDialog() == DialogResult.OK)
                    {
                        colorPanel.BackColor = colorDialog.Color;

                        ChangeColor(tag.Id, colorDialog.Color);

                        flowLayoutPanel1.VerticalScroll.Value = scrollPosition;
                        flowLayoutPanel1.PerformLayout();
                    }
                };
            }
        }
        private void DeleteTag(int tagId, Panel tagPanel)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Удалить записи из таблицы Table_connection_item_Tags, где tag_id равен tagId
                    string deleteConnectionsQuery = "DELETE FROM Table_connection_item_tag WHERE tag_id = @TagId";
                    using (SqlCommand deleteConnectionsCommand = new SqlCommand(deleteConnectionsQuery, connection))
                    {
                        deleteConnectionsCommand.Parameters.AddWithValue("@TagId", tagId);
                        deleteConnectionsCommand.ExecuteNonQuery();
                    }

                    // Удалить запись из таблицы Table_Tags, где tag_id равен tagId
                    string deleteTagQuery = "DELETE FROM Table_Tags WHERE tag_id = @TagId";
                    using (SqlCommand deleteTagCommand = new SqlCommand(deleteTagQuery, connection))
                    {
                        deleteTagCommand.Parameters.AddWithValue("@TagId", tagId);
                        int rowsAffected = deleteTagCommand.ExecuteNonQuery();
                        flowLayoutPanel1.Controls.Remove(tagPanel);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }


        private void ChangeColor(int tagId, Color newColor)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Table_Tags SET color = @Color WHERE tag_id = @TagId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Color", ColorTranslator.ToHtml(newColor));
                        command.Parameters.AddWithValue("@TagId", tagId);
                        int rowsAffected = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}