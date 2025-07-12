using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _18practical
{
    public partial class AddFile : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private string coverFilePath;
        private SqlConnection sqlDB;

        public AddFile()
        {
            InitializeComponent();
            panel1.MouseDown += new MouseEventHandler(panel1_MouseDown);
            panel1.MouseMove += new MouseEventHandler(panel1_MouseMove);
            panel1.MouseUp += new MouseEventHandler(panel1_MouseUp);

            ConnectToSqlDb();
        }

        private void ConnectToSqlDb()
        {
            string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";
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
        private void btnCover_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files (*.jpg, *.png, *.jpeg)|*.jpg;*.png;*.jpeg";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedCoverPath = openFileDialog1.FileName;
            }
        }
        private void btnCreate_Click(object sender, EventArgs e)
        {
            ConnectToSqlDb();

            string fileName = tbName.Text;
            string extension = Path.GetExtension(fileName).ToLower();
            string[] tags = tbTags.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(tag => tag.Trim().ToLower()).ToArray();

            byte[] imageBytes = null;
            byte[] filepathBytes = Encoding.UTF8.GetBytes(fileName);

            if (!string.IsNullOrEmpty(openFileDialog1.FileName))
            {
                imageBytes = File.ReadAllBytes(openFileDialog1.FileName);
            }

            int itemId = InsertItem(fileName, extension, imageBytes, filepathBytes, rtbDescription.Text);

            foreach (string tag in tags)
            {
                int tagId = InsertTag(tag);
                InsertItemTagConnection(itemId, tagId);
            }

            sqlDB.Close();  // Закрываем соединение

            MessageBox.Show("Данные успешно добавлены в базу данных");
            this.Close();
        }

        private int InsertItem(string name, string extension, byte[] image, byte[] filepath, string description)
        {
            string query = "INSERT INTO Table_Items (name, extension, image, filepath, description) OUTPUT INSERTED.item_id VALUES (@name, @extension, @image, @filepath, @description)";
            using (SqlCommand cmd = new SqlCommand(query, sqlDB))
            {
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@extension", extension);
                cmd.Parameters.AddWithValue("@image", image);
                cmd.Parameters.AddWithValue("@filepath", filepath);
                cmd.Parameters.AddWithValue("@description", description);

                return (int)cmd.ExecuteScalar(); // Возвращает добавленный item_id
            }
        }
        private int InsertTag(string name)
        {
            string checkQuery = "SELECT tag_id FROM Table_Tags WHERE name = @name";
            using (SqlCommand checkCmd = new SqlCommand(checkQuery, sqlDB))
            {
                checkCmd.Parameters.AddWithValue("@name", name);
                object result = checkCmd.ExecuteScalar();

                if (result != null)
                {
                    return (int)result; // Возвращает существующий tag_id
                }
                else
                {
                    string insertQuery = "INSERT INTO Table_Tags (name, color) OUTPUT INSERTED.tag_id VALUES (@name, '#400040')";
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, sqlDB))
                    {
                        insertCmd.Parameters.AddWithValue("@name", name);
                        return (int)insertCmd.ExecuteScalar(); // Возвращает добавленный tag_id
                    }
                }
            }
        }

        private void InsertItemTagConnection(int itemId, int tagId)
        {
            string query = "INSERT INTO Table_connection_item_tag (item_id, tag_id) VALUES (@itemId, @tagId)";
            using (SqlCommand cmd = new SqlCommand(query, sqlDB))
            {
                cmd.Parameters.AddWithValue("@itemId", itemId);
                cmd.Parameters.AddWithValue("@tagId", tagId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
