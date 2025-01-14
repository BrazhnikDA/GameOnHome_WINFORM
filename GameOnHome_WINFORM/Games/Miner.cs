﻿using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace GameOnHome_WINFORM.Games
{

    public partial class Miner : Form
    {
        SoundPlayer sound;
        public Miner()
        {
            InitializeComponent();

            sound = new SoundPlayer(Properties.Resources._3Throw);
            sound.Play();

            Init();
        }

        DateTime date1;

        private end_of_game.end_of_game EndGame;

        private const int mapSize = 8;
        private const int cellSize = 75;

        private static int currentPictureToSet = 0;

        private static int[,] map = new int[mapSize, mapSize];

        private static Button[,] buttons = new Button[mapSize, mapSize];

        private static Image spriteSet;

        private static bool isFirstStep;

        private static Point firstCoord;


        private static void InitMap()
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    map[i, j] = 0;
                }
            }
        }

        private void Init()
        {
            this.Width = (mapSize + 1) * cellSize - 57;
            this.Height = (mapSize + 1) * cellSize + 0;

            date1 = new DateTime(0, 0);

            currentPictureToSet = 0;
            isFirstStep = true;
            spriteSet = Properties.Resources.tiles;
            InitMap();
            InitButtons();
        }

        // Созданик кнопок
        private void InitButtons()
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    Button button = new Button();
                    button.Location = new Point(j * cellSize, i * cellSize);
                    button.Anchor = (AnchorStyles.Top | AnchorStyles.Left);
                    button.Size = new Size(cellSize, cellSize);
                    button.Font = new Font(button.Font.FontFamily, 1);
                    button.TabStop = false;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);
                    button.FlatAppearance.BorderSize = 0;
                    button.Image = FindNeededImage(0, 0);
                    button.MouseUp += new MouseEventHandler(OnButtonPressedMouse);
                    
                    buttons[i, j] = button;

                    Controls.Add(button);
                }
            }
        }

        // Обрабатываем нажатие мышкой на кнопку
        // Относительно нажатой клавиши вызываем определённую функцию
        private void OnButtonPressedMouse(object sender, MouseEventArgs e)
        {
            timer1.Start();
            Button pressedButton = sender as Button;
            switch (e.Button.ToString())
            {
                case "Right":
                    OnRightButtonPressed(pressedButton);
                    break;
                case "Left":
                    OnLeftButtonPressed(pressedButton);
                    break;
            }
        }

        // Правая кнопка мыши
        private void OnRightButtonPressed(Button pressedButton)
        {
            currentPictureToSet++;
            currentPictureToSet %= 3;
            int posX = 0;
            int posY = 0;
            switch (currentPictureToSet)
            {
                case 0:
                    posX = 0;
                    posY = 0;
                    break;
                case 1:
                    posX = 0;
                    posY = 2;
                    break;
                case 2:
                    posX = 2;
                    posY = 2;
                    break;
            }
            pressedButton.Image = FindNeededImage(posX, posY);
            if(pressedButton.Text == ".") { pressedButton.Text = ""; }
            CheckWin();
        }

        // Левая кнопка мыши
        private void OnLeftButtonPressed(Button pressedButton)
        {
            pressedButton.Enabled = false;
            int iButton = pressedButton.Location.Y / cellSize;
            int jButton = pressedButton.Location.X / cellSize;
            if (isFirstStep)
            {
                firstCoord = new Point(jButton, iButton);
                SeedMap();
                CountCellBomb();
                isFirstStep = false;
            }
            OpenCells(iButton, jButton);

            if (map[iButton, jButton] == -1)
            {
                timer1.Stop();
                EndGame = new end_of_game.end_of_game("Fail", this);
                EndGame.Owner = this;
                EndGame.Show();
                Init();
            }
            else { CheckWin(); }
        }

        private void Restart()
        {
            Miner newMin = new Miner();
            newMin.Show();
            this.Close();
        }

        // Проверка на победу
        private void CheckWin()
        {
            bool IsWin = true;
            for(int i = 0; i < mapSize; i++)
            {
                for(int j = 0; j < mapSize; j++)
                {
                    if(map[i, j] == -1)
                    {
                        if(buttons[i, j].Text == ".")
                        {
                            IsWin = false;
                            return;
                        }
                    }
                }
            }
            if (IsWin)
            {
                MessageBox.Show("Вы выиграли!");
                Controls.Clear();
                Init();
                sound.Stop();
            }
        }

        // Если открыли бомбу, показываем все бомбы, кроме той которую взорвали
        private static void ShowAllBombs(int iBomb, int jBomb)
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (i == iBomb && j == jBomb)
                        continue;
                    if (map[i, j] == -1)
                    {
                        buttons[i, j].Text = "";
                        buttons[i, j].Image = FindNeededImage(3, 2);
                    }
                }
            }
        }

        // Функция для вырезания нужной картинки из массива картинок 
        private static Image FindNeededImage(int xPos, int yPos)
        {
            Image image = new Bitmap(cellSize, cellSize);
            Graphics g = Graphics.FromImage(image);
            g.DrawImage(spriteSet, new Rectangle(new Point(0, 0), new Size(cellSize, cellSize)), 0 + 32 * xPos, 0 + 32 * yPos, 33, 33, GraphicsUnit.Pixel);

            return image;
        }

        // Создание карты
        private static void SeedMap()
        {
            Random r = new Random();
            int number = r.Next(7, 15);

            for (int i = 0; i < number; i++)
            {
                int posI = r.Next(0, mapSize - 1);
                int posJ = r.Next(0, mapSize - 1);

                while (map[posI, posJ] == -1 || (Math.Abs(posI - firstCoord.Y) <= 1 && Math.Abs(posJ - firstCoord.X) <= 1))
                {
                    posI = r.Next(0, mapSize - 1);
                    posJ = r.Next(0, mapSize - 1);
                }
                map[posI, posJ] = -1;
                buttons[posI, posJ].Text = ".";         // Приписываем кнопкам с бомбой точку, шрифт при создании стоит 1 (Текст не виден)
            }
        }

        // Ячейки с бомбой 
        private static void CountCellBomb()
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] == -1)
                    {
                        for (int k = i - 1; k < i + 2; k++)
                        {
                            for (int l = j - 1; l < j + 2; l++)
                            {
                                if (!IsInBorder(k, l) || map[k, l] == -1)
                                    continue;
                                map[k, l] = map[k, l] + 1;
                            }
                        }
                    }
                }
            }
        }

        // Вспомогательная функция открытия ячейки и занесение нужной картинки
        private static void OpenCell(int i, int j)
        {
            buttons[i, j].Enabled = false;

            switch (map[i, j])
            {
                case 1:
                    buttons[i, j].Image = FindNeededImage(1, 0);
                    break;
                case 2:
                    buttons[i, j].Image = FindNeededImage(2, 0);
                    break;
                case 3:
                    buttons[i, j].Image = FindNeededImage(3, 0);
                    break;
                case 4:
                    buttons[i, j].Image = FindNeededImage(4, 0);
                    break;
                case 5:
                    buttons[i, j].Image = FindNeededImage(0, 1);
                    break;
                case 6:
                    buttons[i, j].Image = FindNeededImage(1, 1);
                    break;
                case 7:
                    buttons[i, j].Image = FindNeededImage(2, 1);
                    break;
                case 8:
                    buttons[i, j].Image = FindNeededImage(3, 1);
                    break;
                case -1:
                    buttons[i, j].Image = FindNeededImage(1, 2);
                    break;
                case 0:
                    buttons[i, j].Image = FindNeededImage(0, 0);
                    break;
            }
        }

        // Функция для открытия ячейки
        private static void OpenCells(int i, int j)
        {
            OpenCell(i, j);

            if (map[i, j] > 0)
                return;

            for (int k = i - 1; k < i + 2; k++)
            {
                for (int l = j - 1; l < j + 2; l++)
                {
                    if (!IsInBorder(k, l))
                        continue;
                    if (!buttons[k, l].Enabled)
                        continue;
                    if (map[k, l] == 0)
                        OpenCells(k, l);
                    else if (map[k, l] > 0)
                        OpenCell(k, l);
                }
            }
        }

        // Проверка на выход за пределы карты
        private static bool IsInBorder(int i, int j)
        {
            if (i < 0 || j < 0 || j > mapSize - 1 || i > mapSize - 1)
            {
                return false;
            }
            return true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            date1 = date1.AddSeconds(1);
            textBox_Timer.Text = date1.ToString("mm:ss");
        }

        private void Miner_Load(object sender, EventArgs e)
        {
            //Label time = new Label();
            //TextBox timebox = new TextBox();
            //Timer timer = new Timer();

            //time.Font = new Font("Segoe UI Emoji", 18f);
            //time.Text = "Время";
            //time.Location = new Point();
        }

        private void Miner_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (EndGame == null)
            {
                ListGames lg = new ListGames();

                if (textBox_Timer.Text != "00:00")
                {
                    DialogResult dialog = MessageBox.Show("Игра только началась. Закрыть окно?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialog == DialogResult.Yes)
                    {
                        sound.Stop();
                        lg.Show();
                    }
                    if(dialog == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    sound.Stop();
                    lg.Show();
                }
            }
            else { sound.Stop(); }
        }
    }
}

