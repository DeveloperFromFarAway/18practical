using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static _18practical.Form1;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Data.SqlClient;
using System.Net;
using System.Security;
using System.Data.Common;

namespace _18practical
{
    /*"О, святой Омниссия, хранитель знания,
    Святи эту машину твоим божественным светом,
    Очисти её от осквернений и дай свое благословение,
    Что бы её работа была точной и безукоризненной.
    От имени Железа и Машины, молитва моя."*/
    public partial class Form1 : Form
    {
        //на это тоже забей
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public List<Item> items = new List<Item>();
        List<Tag> tags = new List<Tag>();
        private readonly string dataDirectory = Path.Combine(Application.StartupPath, "data");

        private List<int> hiddenTagIds = new List<int>();

        private SqlConnection sqlDB;

        private int targetScrollValue = 0;
        public Form1()//основа
        {
            InitializeComponent();
            panel1.MouseDown += new MouseEventHandler(panel1_MouseDown);
            panel1.MouseMove += new MouseEventHandler(panel1_MouseMove);
            panel1.MouseUp += new MouseEventHandler(panel1_MouseUp);

            List<Item> items = LoadItemsFromDatabase();
            LoadTags();

            InitializeFlowLayoutPanel();
            menuStrip1.Renderer = new MyRenderer();
            UpdateItems(items);

            this.Width = GetFormWidthFromSettings();

            ConnectToSqlDb();
            //ShowFirstColumnName("Table_Items");
        }

        ~Form1()
        {
            sqlDB.Close();
        }

        //private readonly char[] password = new[] { '1', 'C', 'o', 'l', 'd', '_', 'L', 'i', 'e', '1' };
        private void ConnectToSqlDb()
        {
            //unsafe
            //{
            //    fixed (char* pw = password)
            //    {
            //        string connectionString = "Server=10.137.203.94;Database=Redas;User ID=22.103k-09;Password=1Cold_Lie1;";

            //        string login = "22.103k-09";
            //        SqlCredential credential = new SqlCredential(login, new SecureString(pw, password.Length));

            //        sqlDB = new SqlConnection(connectionString);
            //    }
            //}
            string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";
            sqlDB = new SqlConnection(connectionString);
            sqlDB.Open();
        }

        //забей на это, код для перетаскивания окна
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

        private int GetFormWidthFromSettings()
        {
            string filePath = Path.Combine(dataDirectory, "style.txt");
            int formWidth = 880;
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("formWidth"))
                    {
                        int.TryParse(line.Split('=')[1].Trim(), out formWidth);
                        break;
                    }
                }
            }
            return formWidth;
        }

        //тут начинается работа с отрисовкой тегов
        public new class Tag
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
        private void LoadTags()
        {
            tags.Clear();

            string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";
            string query = "SELECT t.tag_id, t.name, t.color, STUFF((SELECT ', ' + CAST(c.item_id AS VARCHAR) FROM dbo.Table_connection_item_tag c WHERE c.tag_id = t.tag_id FOR XML PATH('')), 1, 2, '') AS item_ids FROM dbo.Table_Tags t GROUP BY t.tag_id, t.name, t.color;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string tagName = reader.GetString(1);
                    string colorCode = reader.GetString(2);
                    List<int> itemIds = reader.GetString(3).Split(',').Select(int.Parse).ToList();

                    Color tagColor = ColorTranslator.FromHtml(colorCode);
                    Tag newTag = new Tag(id, tagName, tagColor, itemIds);
                    tags.Add(newTag);
                }

                reader.Close();
            }

            // Обновление UI или других компонентов с помощью обновленного списка тегов
            UpdateTags(flowLayoutPanel1, tags, false);
        }
        //начало работы с items и их структурой
        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Extension { get; set; }
            public List<string> Tags { get; set; }
            public byte[] Image { get; set; }
            public byte[] FilePath { get; set; }
            public string Description { get; set; }

            public Item(int itemId, string name, string extension, List<string> tags, byte[] image, byte[] filePath, string description)
            {
                Id = itemId;
                Name = name;
                Extension = extension;
                Tags = tags;
                Image = image;
                FilePath = filePath;
                Description = description;
            }
        }

        private FlowLayoutPanel flowLayoutPanel;//хрен знает что это, потом посмотри если мешать будет
        private void InitializeFlowLayoutPanel()
        {
            flowLayoutPanel2.Dock = DockStyle.Fill;
            flowLayoutPanel2.AutoScroll = true; // включение скроллинга если много элементов
        }

        /*Прими свое благословение,
        изгони все заблуждения,
        чтобы ты мог быть освящен и благословлен,
        и обрети добродетель, которой мы желаем,
        через Святейшее Имя Омниссии,
        чтобы обрел ты действенность и силу,
        Теперь тебе даны понимание и знание,
        позволяющие делать только то, что угодно Тебе
        Истинное существование и Блаженство*/

        //для визуализации тегов
        private void UpdateTags(FlowLayoutPanel flowLayoutPanel, IEnumerable<Tag> tags, bool addCancelSymbol, List<int> hiddenTagIds = null)
        {
            List<Tag> tagsList = new List<Tag>(tags);
            flowLayoutPanel.Controls.Clear(); // Очищаем все текущие контролы

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

                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, 30, 30, 90, 180);
                path.AddArc(tagPanel.Width - 30, 0, 30, 30, 270, 180);
                path.CloseFigure();
                tagPanel.Region = new Region(path);

                string labelText = addCancelSymbol ? $"{tag.Name} ✖️" : tag.Name;

                Label tagLabel = new Label
                {
                    Text = labelText,
                    ForeColor = Color.White,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Tag = tag
                };

                tagPanel.Paint += (sender, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                };

                tagPanel.Controls.Add(tagLabel);
                tagPanel.Click += Tag_Click;
                tagLabel.Click += Tag_Click;

                // Настраиваем видимость панели на основе списка скрытых идентификаторов
                if (hiddenTagIds != null && hiddenTagIds.Contains(tag.Id))
                {
                    tagPanel.Visible = false;
                }

                flowLayoutPanel.Controls.Add(tagPanel); // Добавляем панель в переданный flowLayoutPanel
            }
        }

        //для визуализации айтемов
        private void UpdateItems(IEnumerable<Item> items)
        {
            flowLayoutPanel2.Controls.Clear();

            // Чтение данных из файла style.txt
            string filePath = Path.Combine(dataDirectory, "style.txt");
            int sizeItem = 150; // значение по умолчанию
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("SizeItem"))
                    {
                        int.TryParse(line.Split('=')[1].Trim(), out sizeItem);
                        break;
                    }
                }
            }

            foreach (var item in items)
            {
                int count = items.Count();
                // главная панель
                Panel mainPanel = new Panel
                {
                    Size = new Size(sizeItem, sizeItem)
                };

                mainPanel.BackgroundImage = null; // Отключаем автоматическую отрисовку фона
                mainPanel.BackgroundImageLayout = ImageLayout.None;

                mainPanel.Paint += (sender, e) =>
                {
                    if (item.Image != null && item.Image.Length > 0)
                    {
                        using (Image image = ByteArrayToImage(item.Image))
                        {
                            float imageAspectRatio = (float)image.Width / image.Height;
                            float panelAspectRatio = (float)mainPanel.Width / mainPanel.Height;

                            int displayWidth, displayHeight;
                            if (imageAspectRatio > panelAspectRatio)
                            {
                                // Изображение шире панели
                                displayHeight = mainPanel.Height;
                                displayWidth = (int)(image.Width * ((float)mainPanel.Height / image.Height));
                            }
                            else
                            {
                                // Изображение уже панели
                                displayWidth = mainPanel.Width;
                                displayHeight = (int)(image.Height * ((float)mainPanel.Width / image.Width));
                            }

                            // Рассчитываем, чтобы центрировать изображение
                            int x = (mainPanel.Width - displayWidth) / 2;
                            int y = (mainPanel.Height - displayHeight) / 2;

                            // Отрисовка изображения
                            e.Graphics.DrawImage(image, x, y, displayWidth, displayHeight);
                        }
                    }
                };


                // нижняя черная панель с уменьшенной высотой
                Panel bottomPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 15,
                    BackColor = Color.Black,
                    Width = flowLayoutPanel2.ClientRectangle.Width  // Задаем ширину панели
                };
                // лейбл с именем файла (по левому краю)
                string trimmedName = TrimFilenameToFit(item.Name, sizeItem, SystemFonts.DefaultFont);
                Label nameLabel = new Label
                {
                    Text = trimmedName,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize = true
                };
                nameLabel.Dock = DockStyle.Left;

                // лейбл с расширением файла (по правому краю)
                Label extensionLabel = new Label
                {
                    Text = item.Extension,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = true
                };
                extensionLabel.Padding = new Padding(0, (bottomPanel.Height - extensionLabel.Height) / 2, 0, 0); // Центрирование по вертикали
                extensionLabel.Dock = DockStyle.Right;

                // Добавление элементов управления на панели
                bottomPanel.Controls.Add(nameLabel);
                bottomPanel.Controls.Add(extensionLabel);
                mainPanel.Controls.Add(bottomPanel);
                flowLayoutPanel2.Controls.Add(mainPanel);

                mainPanel.Click += (sender, e) =>
                {
                    OpenItemForm(item);
                };
            }
        }
        private Image ByteArrayToImage(byte[] byteArray)
        {
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                return Image.FromStream(ms);
            }
        }
        private string TrimFilenameToFit(string filename, int panelWidth, Font font)
        {
            float maxSize = panelWidth * 0.7f;
            Size size = TextRenderer.MeasureText(filename, font); // Измеряем текст
            double textsize = size.Width;

            // Проверяем, достаточно ли места для отображения полного имени файла
            if (textsize <= maxSize)
            {
                return filename; // Если текст меньше или равен 70% ширины, возвращаем полное имя
            }

            // Проверяем, когда текст стал занимать меньше или равно 70% ширины панели
            while (size.Width > maxSize)
            {
                if (filename.Length <= 2)
                {
                    return "..."; // Если осталось два символа или меньше, возвращаем троеточие
                }

                filename = filename.Substring(0, filename.Length - 3); // Уменьшаем имя файла на два символа
                size = TextRenderer.MeasureText(filename + "...", font); // Измеряем размер с добавлением троеточия
            }

            return filename + "..."; // Добавляем троеточие к укороченному имени и возвращаем его
        }

        private List<Label> activeLabels = new List<Label>();
        List<Tag> activeTags = new List<Tag>();
        List<Item> activeItems = new List<Item>();
        //обработка клика по тегу
        private void Tag_Click(object sender, EventArgs e)
        {
            Label clickedLabel = sender as Label;
            Tag tag = clickedLabel?.Tag as Tag;

            if (clickedLabel == null || tag == null) return;

            bool wasActive = clickedLabel.Text.EndsWith("✖️");
            clickedLabel.Text = wasActive ? clickedLabel.Text.Substring(0, clickedLabel.Text.Length - 2) : clickedLabel.Text + "✖️";

            if (wasActive)
            {
                activeTags.Remove(tag);
                tags.Add(tag);
            }
            else
            {
                activeTags.Add(tag);
                tags.Remove(tag);
            }
            UpdateTags(flowLayoutPanel3, activeTags, true);
            UpdateTags(flowLayoutPanel1, tags, false); 

            UpdateTags(flowLayoutPanel3, activeTags, true, hiddenTagIds);
            UpdateTags(flowLayoutPanel1, tags, false, hiddenTagIds); // сделал на отъебись лишь бы заработало, а оно правда заработало как надо, хех
            RecalculateActiveItems();
        }
        private void RecalculateActiveItems()
        {
            if (activeTags.Count == 0)
            {
                UpdateItems(items); // Если нет активных тегов, отображаем полный список items
            }
            else
            {
                HashSet<int> activeItemIds = new HashSet<int>(activeTags[0].ItemIds);

                foreach (Tag tag in activeTags.Skip(1))
                {
                    activeItemIds.IntersectWith(tag.ItemIds);
                }

                // Фильтруем items, оставляя только те, чьи ID содержатся в activeItemIds
                activeItems = items.Where(item => activeItemIds.Contains(item.Id)).ToList();

                UpdateItems(activeItems); // Обновляем отображаемые элементы
            }
        }


        private void tbSetch_TextChanged(object sender, EventArgs e)
        {
            FilterTags(tbSetch.Text);
        }
        private void FilterTags(string searchText)
        {
            // Очистка списка с каждым вызовом фильтрации, чтобы хранить только актуальные id для данного поиска
            hiddenTagIds.Clear();

            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is Panel tagPanel && tagPanel.Controls.Count > 0)
                {
                    var tagLabel = tagPanel.Controls[0] as Label;
                    if (tagLabel != null && tagLabel.Tag is Tag tag)
                    {
                        // Проверяем условие видимости
                        bool isVisible = string.IsNullOrEmpty(searchText) || tag.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                        // Устанавливаем видимость панели
                        tagPanel.Visible = isVisible;

                        // Если панель не видима, добавляем id в список
                        if (!isVisible)
                        {
                            hiddenTagIds.Add(tag.Id);
                        }
                    }
                }
            }
        }

        private void файлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddFile createForm = new AddFile();
            createForm.ShowDialog();
        }

        private List<Item> LoadItemsFromDatabase()
        {
            items.Clear();

            string query = @"
            SELECT 
                i.item_id, 
                i.name, 
                i.extension, 
                i.image, 
                i.filepath, 
                i.description, 
                STUFF(
                    (SELECT ', ' + CAST(c.tag_id AS VARCHAR)
                        FROM dbo.Table_connection_item_tag c
                        WHERE c.item_id = i.item_id 
                        FOR XML PATH('')), 1, 2, '') AS tag_ids
            FROM 
                dbo.Table_Items i
            GROUP BY 
                i.item_id, i.name, i.extension, i.image, i.filepath, i.description";

            string connectionString = "Server=127.0.0.1;Database=Redas;Integrated Security=True;";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    string[] parts = name.Split('.');
                    name = parts[0];
                    string extension = reader.GetString(2);
                    byte[] image = reader.IsDBNull(3) ? null : (byte[])reader[3];
                    byte[] filePath = reader.IsDBNull(4) ? null : (byte[])reader[4];
                    string description = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                    List<string> tags = string.IsNullOrEmpty(reader.GetString(6)) ? new List<string>() : new List<string>(reader.GetString(6).Split(','));

                    items.Add(new Item(id, name, extension, tags, image, filePath, description));
                }
            }

            return items;
        }

        private void редактироватьТегиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditingTags editingtags = new EditingTags();
            editingtags.ShowDialog();
        }
        private void update_click(object sender, EventArgs e)
        {
            activeTags.Clear();
            RecalculateActiveItems();
            LoadTags();
            LoadItemsFromDatabase();
            UpdateItems(items);
            this.Width = GetFormWidthFromSettings();
        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings settings = new settings();
            settings.ShowDialog();
        }

        private void OpenItemForm(Item item)
        {
            switch (item.Extension)
            {
                case ".txt":
                    textFile text = new textFile(item);
                    text.ShowDialog();
                    break;
                case ".jpg":
                    imageFile image = new imageFile(item);
                    image.ShowDialog();
                    break;
                default:
                    MessageBox.Show("Низвестное расширение");
                    break;
            }
        }
    }
    //внешка меню с menuStrip1, допиши после
    public class MyRenderer : ToolStripProfessionalRenderer
    {
        private readonly Brush backgroundColorBrush = new SolidBrush(ColorTranslator.FromHtml("#241B2D"));
        private readonly Brush selectedColorBrush = new SolidBrush(ColorTranslator.FromHtml("#564F6F"));
        private readonly Color textColor = ColorTranslator.FromHtml("#EDEDED");
        private readonly Font textFont = new Font("Segoe UI", 9, FontStyle.Bold);
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.FillRectangle(backgroundColorBrush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Enabled)
            {
                if (e.Item.Selected)
                {
                    e.Graphics.FillRectangle(selectedColorBrush, e.Item.Bounds);
                }
                else
                {
                    e.Graphics.FillRectangle(backgroundColorBrush, e.Item.Bounds);
                }
            }
            else // Рендер для неактивных элементов
            {
                e.Graphics.FillRectangle(Brushes.Gray, e.Item.Bounds);
            }
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? textColor : Color.Gray; // Цвет текста для неактивных элементов
            e.TextFont = textFont;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            // Центральная линия сепаратора
            int midX = e.Item.Bounds.Width / 2;
            e.Graphics.DrawLine(Pens.Gray, midX, 0, midX, e.Item.Bounds.Height);
        }
    }
    public class MyCustomControl : Control
    {
        public MyCustomControl()
        {
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
        }
    }
}