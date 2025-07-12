using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static _18practical.Form1;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace _18practical
{
    public partial class imageFile : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private readonly string dataDirectory = Path.Combine(Application.StartupPath, "data");
        public imageFile(Item item)
        {
            InitializeComponent();
            panel1.MouseDown += new MouseEventHandler(panel1_MouseDown);
            panel1.MouseMove += new MouseEventHandler(panel1_MouseMove);
            panel1.MouseUp += new MouseEventHandler(panel1_MouseUp);

            MyForm_Load(this, EventArgs.Empty, item);
            FillFormWithData(item);
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
        public class Tag
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Color Color { get; set; }
            public List<int> ItemIds { get; set; } // Изменение на список идентификаторов элементов

            public Tag(int id, string name, Color color, List<int> itemIds = null)
            {
                Id = id;
                Name = name;
                Color = color;
                ItemIds = itemIds ?? new List<int>();  // Инициализация списка, если аргумент не предоставлен
            }
        }

        private List<Tag> tagsList = new List<Tag>();
        private void UpdateTags(IEnumerable<Tag> tags, bool showAll = false)
        {
            tagsList = new List<Tag>(tags);
            flowLayoutPanel1.Controls.Clear();

            int currentWidth = 0;
            bool showMoreAdded = false;

            foreach (var tag in tagsList)
            {
                Label tempLabel = new Label
                {
                    Text = tag.Name,
                    AutoSize = true,
                    Font = new Font("Arial", 10)
                };

                Size textSize;
                using (var g = tempLabel.CreateGraphics())
                {
                    textSize = Size.Ceiling(g.MeasureString(tempLabel.Text, tempLabel.Font));
                }

                Panel tagPanel = new Panel
                {
                    Size = new Size(textSize.Width + 10, 30),
                    BackColor = tag.Color,
                    Tag = tag
                };

                tagPanel.Region = CreateRoundedRectangleRegion(tagPanel.Size);

                Label tagLabel = new Label
                {
                    Text = tag.Name,
                    ForeColor = Color.White,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Tag = tag
                };

                tagPanel.Controls.Add(tagLabel);

                currentWidth += tagPanel.Width;
                if (!showAll && currentWidth > flowLayoutPanel1.Width && !showMoreAdded)
                {
                    if (showMoreAdded == false)
                    {
                        AddShowMoreButton();
                        showMoreAdded = true;
                    }
                    break;
                }

                flowLayoutPanel1.Controls.Add(tagPanel);
            }
        }

        private void ShowMore_Click(object sender, EventArgs e)
        {
            UpdateTags(tagsList, true);
        }

        private void AddShowMoreButton()
        {
            Button showMore = new Button
            {
                Text = "Show more",
                Width = 100,
                Height = 30,
                Margin = new Padding(3),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            showMore.FlatAppearance.BorderSize = 0;
            showMore.Region = CreateRoundedRectangleRegion(showMore.Size);
            showMore.Click += ShowMore_Click;

            flowLayoutPanel1.Controls.Add(showMore);
        }

        private Region CreateRoundedRectangleRegion(Size size)
        {
            // Создание Region с закругленными углами для панели
            GraphicsPath path = new GraphicsPath();
            int radius = size.Height / 2;  // Радиус равен половине высоты для создания полукруглых краев
            path.AddArc(0, 0, 2 * radius, 2 * radius, 90, 180);  // Левый верхний угол
            path.AddArc(size.Width - 2 * radius, 0, 2 * radius, 2 * radius, 270, 180);  // Правый верхний угол
            path.CloseAllFigures();

            return new Region(path);
        }
        private void MyForm_Load(object sender, EventArgs e, Item item)
        {
            List<string> tagsID = new List<string>(item.Tags);
            List<Tag> matchedTags = FindMatchingTags(tagsID);
            UpdateTags(matchedTags);
        }

        private List<Tag> FindMatchingTags(List<string> tagsID)
        {
            List<int> tagsIdInt = tagsID.Select(id => int.Parse(id)).ToList();
            List<Tag> matchedTags = new List<Tag>();

            string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";
            string query = "SELECT tag_id, name, color FROM Table_Tags WHERE tag_id IN (" + string.Join(",", tagsIdInt) + ")";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int tagID = reader.GetInt32(0);
                    string tagName = reader.GetString(1);
                    Color tagColor = ColorTranslator.FromHtml(reader.GetString(2));

                    // Поскольку у нас нет информации о поле itemIds в таблице, мы создаем пустой список.
                    List<int> itemIds = new List<int>();

                    matchedTags.Add(new Tag(tagID, tagName, tagColor, itemIds));
                }
            }

            return matchedTags;
        }
        public void FillFormWithData(Item item)
        {
            lName.Text = $"{item.Name}{item.Extension}";
            rtbDescription.Text = item.Description;
            SetPanelBackgroundImage(item);
        }
        private Image ByteArrayToImage(byte[] byteArray)
        {
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                return Image.FromStream(ms);
            }
        }

        public void SetPanelBackgroundImage(Item item)
        {
            if (item?.Image == null || item.Image.Length == 0)
            {
                return;
            }

            try
            {
                // Преобразуем байтовый массив в изображение
                Image image = ByteArrayToImage(item.Image);
                pImage.BackgroundImage = image;
                // Устанавливаем значение Zoom для BackgroundImageLayout
                pImage.BackgroundImageLayout = ImageLayout.Zoom;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
