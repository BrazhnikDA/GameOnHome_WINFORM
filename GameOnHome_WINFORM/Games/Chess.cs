﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Media;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GameOnHome_WINFORM.Games
{
    public partial class Chess : Form
    {
        private end_of_game.end_of_game EndGame;

        SoundPlayer sound;                          // Музыка

        public string ID;                          // ID присвоенное сервером
        public bool IsStatus = false;              // True - онлайн, False - оффлайн

        private const string host = "127.0.0.1";    // IP
        private const int port = 7770;              // Порт
        TcpClient client;                           // Клиент
        NetworkStream stream;                       // Поток от клиента до сервера

        private const int mapSize = 8;

        private int[,] map = new int[mapSize, mapSize];   // Карта

        private Button[,] buttons = new Button[mapSize, mapSize];  // Все кнопки
        private List<Color> colorMap = new List<Color>();

        private int cellSize = 150;

        private int currentPlayer = 0;                  // Текущий игрок

        private Button pressedButton;                // Нажатая кнопка
        private Button prevButton;                   // Предыдущая нажатая кнопка

        private bool isMoving = false;               // Есть ли куда сходить
        private bool isRakirovka = false;            // Делал ли игрок ракировку

        private List<int[]> stepsBot = new List<int[]>();  // Список возможных ходов бота
        private List<int[]> stepsPlayer = new List<int[]>();  // Список возможных ходов игрока
        private List<int[]> pathToKing = new List<int[]>();  // Список ходов до шаха

        private Image whiteKing;                      
        private Image whiteQueen;                      
        private Image whiteElephant;                      
        private Image whiteHorse;                      
        private Image whiteTower;                      
        private Image whitePawn;

        private Image blackKing;
        private Image blackQueen;
        private Image blackElephant;
        private Image blackHorse;
        private Image blackTower;
        private Image blackPawn;

        public bool GetStatus
        {
            get { return IsStatus; }
        }
        public Chess(bool IsStatus_)
        {
            InitializeComponent();

            sound = new SoundPlayer(Properties.Resources.fonMusic);
            sound.Play();

            this.Name = "Chess";

            IsStatus = IsStatus_;

            Initialization();
            InitImage();
            CreateMap();

            if (IsStatus)
            {
                Server_Connect();       // Функция для подключеняи к серверу
                Thread.Sleep(100);      // Ожидаем подключения
            }
        }

        private void Initialization()
        {
            // Белые:  0 - пустота, 11 - король, 12 - королева, 13 - слон, 14 - конь, 15 - ладья, 16 - пешка
            // Чёрные: 0 - пустота, 21 - король, 22 - королева, 23 - слон, 24 - конь, 25 - ладья, 26 - пешка 
            map = new int[mapSize, mapSize]
            {
            {15,14,13,12,11,13,14,15 },
            {16,16,16,16,16,16,16,16 },
            {0,0,0,0,0,0,0,0 },
            {0,0,0,0,0,0,0,0 },
            {0,0,0,0,0,0,0,0 },
            {0,0,0,0,0,0,0,0 },
            {26,26,26,26,26,26,26,26 },
            {25,24,23,22,21,23,24,25 },
            };
        }

        private void InitImage()
        {
          whiteKing = Properties.Resources.whiteKing;
          whiteQueen = Properties.Resources.whiteQueen;
          whiteElephant = Properties.Resources.whiteElephant;
          whiteHorse = Properties.Resources.whiteHorse;
          whiteTower = Properties.Resources.whiteTower;
          whitePawn = Properties.Resources.whitePawn;

          blackKing = Properties.Resources.blackKing;
          blackQueen = Properties.Resources.blackQueen;
          blackElephant = Properties.Resources.blackElephant;
          blackHorse = Properties.Resources.blackHorse;
          blackTower = Properties.Resources.blackTower;
          blackPawn = Properties.Resources.blackPawn;
    }

        private void CreateMap()
        {
            this.Width = (mapSize + 1) * cellSize - 120;
            this.Height = (mapSize + 1) * cellSize - 88;

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    Button butt = new Button();
                    butt.Name = "button" + i.ToString() + "_" + j.ToString();
                    butt.Size = new Size(cellSize, cellSize);
                    butt.Location = new Point(j * cellSize, i * cellSize);
                    butt.BackColor = GetPrevButtonColor(butt);
                    if (IsStatus)
                        butt.Click += new EventHandler(FigureClickOnline);
                    else butt.Click += new EventHandler(FigureClickOffline);
                    butt.Image = GetImage(map[i, j]);

                    colorMap.Add(butt.BackColor);
                    this.Controls.Add(butt);

                    buttons[i, j] = butt;
                }
            }
        }

        public void Restart()
        {
            this.Controls.Clear();
            stepsBot.Clear();
            stepsPlayer.Clear();
            pathToKing.Clear();
            isMoving = false;
            isRakirovka = false;
            currentPlayer = 0;
            Initialization();
            CreateMap();
        }

        private void FigureClickOffline(object sender, EventArgs e)
        {
            if (currentPlayer == 0) { currentPlayer = 1; }
            pressedButton = sender as Button;

            if (GetColorFigure(CheckMap(ConvertNameI(pressedButton), ConvertNameY(pressedButton))) != 0 &&
                GetColorFigure(CheckMap(ConvertNameI(pressedButton), ConvertNameY(pressedButton))) == currentPlayer)
            {
                // Очищаем поле после второго нажатие на ту же фигуру
                if (pressedButton.BackColor == Color.Red)
                {
                    pressedButton.BackColor = GetPrevButtonColor(pressedButton);
                    RefreshColorMap();
                    ActivateAllButtons();
                    isMoving = false;
                    return;
                }
                RefreshColorMap();
                DeactivateAllButtons();
                pressedButton.BackColor = Color.Red;        // Выделяем нажатую кнопку красным
                pressedButton.Enabled = true;
                ShowSteps(ConvertNameI(pressedButton), ConvertNameY(pressedButton));

                if (isMoving)
                {
                    RefreshColorMap();
                    pressedButton.BackColor = GetPrevButtonColor(pressedButton);
                    ShowSteps(ConvertNameI(pressedButton), ConvertNameY(pressedButton));    // Показываем куда можем сходить
                    isMoving = false;

                }
                else
                {
                    isMoving = true;
                }
            }
            else
            {
                if (isMoving)
                {
                    if (pressedButton.BackColor == Color.Yellow)
                    {
                        map[ConvertNameI(pressedButton), ConvertNameY(pressedButton)] = map[ConvertNameI(prevButton), ConvertNameY(prevButton)];
                        map[ConvertNameI(prevButton), ConvertNameY(prevButton)] = 0;
                        pressedButton.Image = prevButton.Image;
                        prevButton.Image = null;
                        prevButton.BackColor = Color.White;

                        isMoving = false;
                        RefreshColorMap();

                        CheckWin();
                        BotChoosingImportingMove(map);
                        //if(pathToKing.Count > 0) { MessageBox.Show("Путь до короля есть"); }
                        Thread brainBot = new Thread(new ThreadStart(BotBrainEasy));
                        brainBot.Start(); //старт потока
                        while(brainBot.ThreadState != ThreadState.WaitSleepJoin) { }
                        CheckWin();
                        ActivateAllButtons();
                        //CheckShah();
                        stepsPlayer.Clear();
                    }
                }
            }
            prevButton = pressedButton;
        }
        
        // Записываем путь до короля в список
        private int[] GetPathToKing(int i, int j, int ii, int jj)
        {
            switch(map[i, j])
            {
                case 12:
                    if (ii > i && jj == j)   // Вверх
                    {
                        while (i != ii)
                        {
                            pathToKing.Add(new int[5] { i, j, ++i, j, 0 });
                        }
                    }
                    if (ii < i && jj == j)   // Вниз
                    {
                        while (i != ii)
                        {
                            pathToKing.Add(new int[5] { i, j, --i, j, 0 });
                        }
                    }
                    if (jj > j && ii == i)   // Вправо
                    {
                        while (jj != j)
                        {
                            pathToKing.Add(new int[5] { i, j, i, ++j, 0 });
                        }
                    }
                    if (jj < j && ii == i)   // Влево
                    {
                        while (jj != j)
                        {
                            pathToKing.Add(new int[5] { i, j, i, --j, 0 });
                        }
                    }
                    if (ii > i)
                    {
                        if(jj < j)
                        {
                            while(i != ii)
                            {
                                pathToKing.Add(new int[5] { i, j, ++i, --j, 0 });
                            }
                        }else
                        {
                            while (i != ii)
                            {
                                pathToKing.Add(new int[5] { i, j, ++i, ++j, 0 });
                            }
                        }
                    }
                    break;
                case 13:
                    if (ii > i)
                    {
                        if (jj < j)
                        {
                            while (i != ii)
                            {
                                pathToKing.Add(new int[5] { i, j, ++i, --j, 0 });
                            }
                        }
                        else
                        {
                            while (i != ii)
                            {
                                pathToKing.Add(new int[5] { i, j, ++i, ++j, 0 });
                            }
                        }
                    }
                    break;
                case 15:
                    if(ii > i && jj == j)   // Вверх
                    {
                        while(i != ii)
                        {
                            pathToKing.Add(new int[5] { i, j, ++i, j, 0 });
                        }
                    }
                    if(ii < i && jj == j)   // Вниз
                    {
                        while(i != ii)
                        {
                            pathToKing.Add(new int[5] { i, j, --i, j, 0 });
                        }
                    }
                    if(jj > j && ii == i)   // Вправо
                    {
                        while(jj != j)
                        {
                            pathToKing.Add(new int[5] { i, j, i, ++j, 0 });
                        }
                    }
                    if(jj < j && ii == i)   // Влево
                    {
                        while(jj != j)
                        {
                            pathToKing.Add(new int[5] { i, j, i, --j, 0 });
                        }
                    }
                    break;
                case 16:
                    pathToKing.Add(new int[5] { i, j, ii, jj, 0 });
                    break;
            }
            return new int[4] { 0,0,0,0};
        }

        private void BotBrainEasy()
        {
            stepsBot.Clear(); // Очищаем ходы
            int isCloseShax = 0;       // Закрыт ли поставленный шах
            int[] isShax = new int[5]; 
            isShax = CheckShah();
            if (isShax[0] != -1)
            {
                GetPathToKing(isShax[0], isShax[1], isShax[2], isShax[3]);
            }
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] > 20)
                    {
                        BotCheckEat(i, j);
                        BotCheckStep(i, j);
                    }
                }
            }
            if(stepsBot.Count > 0)
            {
                List<int> steps = new List<int>();
                // Ищем самый "важный" ход
                int min = stepsBot[0][4];
                int indexEat = 0;
                for(int i = 1; i < stepsBot.Count; i++)
                {
                    if(min >= stepsBot[i][4] && stepsBot[i][4] < 100) 
                    { 
                        min = stepsBot[i][4]; indexEat = i; 
                    }
                    if(stepsBot[i][4] > 100) 
                    { 
                        steps.Add(i); 
                    }
                }

                if (isShax[0] != -1)
                {
                    // Закрываемся от шаха фигурой
                    if (isShax[0] != -1 && pathToKing.Count > 0)
                    {
                        for (int i = 0; i < stepsBot.Count; i++)
                        {
                            if (isCloseShax == 1)
                            {
                                break;
                            }
                            // Закрываем шах съеданием фигуры
                            for (int j = 0; j < pathToKing.Count; j++)
                            {
                                if (isCloseShax == 1)
                                {
                                    break;
                                }
                                if (stepsBot[i][2] == pathToKing[j][2] && stepsBot[i][3] == pathToKing[j][3])
                                {
                                    if (stepsBot[i][0] != isShax[2] && stepsBot[i][1] != isShax[3])
                                    {
                                        map[stepsBot[i][2], stepsBot[i][3]] = map[stepsBot[i][0], stepsBot[i][1]];
                                        map[stepsBot[i][0], stepsBot[i][1]] = 0;

                                        buttons[stepsBot[i][2], stepsBot[i][3]].Image = buttons[stepsBot[i][0], stepsBot[i][1]].Image;
                                        buttons[stepsBot[i][0], stepsBot[i][1]].Image = null;

                                        isCloseShax = 1;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isCloseShax == 0)
                        {
                            for (int i = 0; i < stepsBot.Count; i++)
                            {
                                for (int j = 0; j < pathToKing.Count; j++)
                                {
                                    if (stepsBot[i][2] != pathToKing[j][2] && stepsBot[i][3] != pathToKing[j][3] && map[stepsBot[i][0], stepsBot[i][1]] == 21)
                                    {
                                        map[stepsBot[i][2], stepsBot[i][3]] = map[stepsBot[i][0], stepsBot[i][1]];
                                        map[stepsBot[i][0], stepsBot[i][1]] = 0;

                                        buttons[stepsBot[i][2], stepsBot[i][3]].Image = buttons[stepsBot[i][0], stepsBot[i][1]].Image;
                                        buttons[stepsBot[i][0], stepsBot[i][1]].Image = null;

                                        isCloseShax = 1;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isCloseShax == 0)
                        {
                            for (int i = 0; i < stepsBot.Count; i++)
                            {
                                if (stepsBot[i][0] == isShax[2] && stepsBot[i][1] == isShax[3])
                                {

                                    map[stepsBot[i][2], stepsBot[i][3]] = map[stepsBot[i][0], stepsBot[i][1]];
                                    map[stepsBot[i][0], stepsBot[i][1]] = 0;

                                    buttons[stepsBot[i][2], stepsBot[i][3]].Image = buttons[stepsBot[i][0], stepsBot[i][1]].Image;
                                    buttons[stepsBot[i][0], stepsBot[i][1]].Image = null;

                                    isCloseShax = 1;
                                    break;
                                }
                            }
                        }
                    }
                    if (isCloseShax != 1)
                        isCloseShax = -1;
                }
                else
                {
                    if (indexEat == 0)
                    {
                        Random rnd = new Random();
                        int xod = rnd.Next(0, steps.Count);

                        // Нет шаха сьедаем важную фигуру
                        map[stepsBot[xod][2], stepsBot[xod][3]] = map[stepsBot[xod][0], stepsBot[xod][1]];    // Куда сходили теперь наша фигура
                        map[stepsBot[xod][0], stepsBot[xod][1]] = 0;    // Где стояли теперь пустота

                        buttons[stepsBot[xod][2], stepsBot[xod][3]].Image = buttons[stepsBot[xod][0], stepsBot[xod][1]].Image;
                        buttons[stepsBot[xod][0], stepsBot[xod][1]].Image = null;

                    }
                    else
                    {

                        map[stepsBot[indexEat][2], stepsBot[indexEat][3]] = map[stepsBot[indexEat][0], stepsBot[indexEat][1]];    // Куда сходили теперь наша фигура
                        map[stepsBot[indexEat][0], stepsBot[indexEat][1]] = 0;    // Где стояли теперь пустота

                        buttons[stepsBot[indexEat][2], stepsBot[indexEat][3]].Image = buttons[stepsBot[indexEat][0], stepsBot[indexEat][1]].Image;
                        buttons[stepsBot[indexEat][0], stepsBot[indexEat][1]].Image = null;

                    }
                }
            }
            if(isCloseShax == -1) { MessageBox.Show("Game over"); }
            Thread.Sleep(220);
        }

        private void BotCheckStep(int i, int j)
        {
            switch (map[i, j])
            {
                case 21:
                    BotCheckStepKing(i, j);
                    break;
                case 22:
                    BotCheckStepQueen(i, j);
                    break;
                case 23:
                    BotCheckStepElephant(i, j);
                    break;
                case 24:
                    BotCheckStepHorse(i, j);
                    break;
                case 25:
                    BotCheckStepTower(i, j);
                    break;
                case 26:
                    BotCheckStepPawn(i, j);
                    break;
            }
        }

        private void BotCheckStepKing(int i, int j)
        {
            if (IsInsideBorders(i + 1, j))
            {
                if (map[i + 1, j] == 0)
                {
                    AddStep(i, j, i + 1, j, 100 + GetTypeFigure(map[i + 1, j]));
                }
            }

            if (IsInsideBorders(i - 1, j))
            {
                if (map[i - 1, j] == 0)
                {
                    AddStep(i, j, i - 1, j, 100 + GetTypeFigure(map[i - 1, j]));
                }
            }

            if (IsInsideBorders(i, j + 1))
            {
                if (map[i, j + 1] == 0)
                {
                    AddStep(i, j, i, j + 1, 100 + GetTypeFigure(map[i, j + 1]));
                }
            }

            if (IsInsideBorders(i, j - 1))
            {
                if (map[i, j - 1] == 0)
                {
                    AddStep(i, j, i, j - 1, 100 + GetTypeFigure(map[i, j - 1]));
                }
            }

            if (IsInsideBorders(i + 1, j + 1))
            {
                if (map[i + 1, j + 1] == 0)
                {
                    AddStep(i, j, i + 1, j + 1, 100 + GetTypeFigure(map[i + 1, j + 1]));
                }
            }

            if (IsInsideBorders(i - 1, j + 1))
            {
                if (map[i - 1, j + 1] == 0)
                {
                    AddStep(i, j, i - 1, j + 1, 100 + GetTypeFigure(map[i - 1, j + 1]));
                }
            }

            if (IsInsideBorders(i + 1, j - 1))
            {
                if (map[i + 1, j - 1] == 0)
                {
                    AddStep(i, j, i + 1, j - 1, 100 + GetTypeFigure(map[i + 1, j - 1]));
                }
            }

            if (IsInsideBorders(i - 1, j - 1))
            {
                if (map[i - 1, j - 1] == 0)
                {
                    AddStep(i, j, i - 1, j - 1, 100 + GetTypeFigure(map[i - 1, j - 1]));
                }
            }
        }

        private void BotRakirovka()
        {
            if (!isRakirovka)
            {
                if (map[7, 0] == 25 && map[7, 7] == 25 && map[7,4] == 21)
                {

                }
            }
        }

        private void BotCheckStepQueen(int i, int j)
        {
            BotCheckStepElephant(i, j);
            BotCheckStepTower(i, j);
        }

        private void BotCheckStepElephant(int i, int j)
        {
            int ii = i - 1;
            int jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вверх-влево
            {
                if (map[ii, jj] == 0)
                {
                    AddStep(i, j, ii, jj, 100 + GetTypeFigure(map[ii, jj]));
                }
                else { break; }
                ii--; jj--;
            }

            ii = i + 1;
            jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вниз-влево
            {
                if (map[ii, jj] == 0)
                {
                    AddStep(i, j, ii, jj, 100 + GetTypeFigure(map[ii, jj]));
                }
                else { break; }
                ii++; jj--;
            }

            ii = i - 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вверх-вправо
            {
                if (map[ii, jj] == 0)
                {
                    AddStep(i, j, ii, jj, 100 + GetTypeFigure(map[ii, jj]));
                }
                else { break; }
                ii--; jj++;
            }

            ii = i + 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вниз-вправо
            {
                if (map[ii, jj] == 0)
                {
                    AddStep(i, j, ii, jj, 100 + GetTypeFigure(map[ii, jj]));
                }
                else { break; }
                ii++; jj++;
            }
        }

        private void BotCheckStepHorse(int i, int j)
        {
            if (IsInsideBorders(i - 2, j + 1))
            {
                if (map[i - 2, j + 1] == 0)
                {
                    AddStep(i, j, i - 2, j + 1, 100 + GetTypeFigure(map[i - 2, j + 1]));
                }
            }
            if (IsInsideBorders(i - 2, j - 1))
            {
                if (map[i - 2, j - 1] == 0)
                {
                    AddStep(i, j, i - 2, j - 1, 100 + GetTypeFigure(map[i - 2, j - 1]));
                }
            }
            if (IsInsideBorders(i + 2, j + 1))
            {
                if (map[i + 2, j + 1] == 0)
                {
                    AddStep(i, j, i + 2, j + 1, 100 + GetTypeFigure(map[i + 2, j + 1]));
                }
            }
            if (IsInsideBorders(i + 2, j - 1))
            {
                if (map[i + 2, j - 1] == 0)
                {
                    AddStep(i, j, i + 2, j - 1, 100 + GetTypeFigure(map[i + 2, j - 1]));
                }
            }
            if (IsInsideBorders(i - 1, j + 2))
            {
                if (map[i - 1, j + 2] == 0)
                {
                    AddStep(i, j, i - 1, j + 2, 100 + GetTypeFigure(map[i - 1, j + 2]));
                }
            }
            if (IsInsideBorders(i + 1, j + 2))
            {
                if (map[i + 1, j + 2] == 0)
                {
                    AddStep(i, j, i + 1, j + 2, 100 + GetTypeFigure(map[i + 1, j + 2]));
                }
            }
            if (IsInsideBorders(i - 1, j - 2))
            {
                if (map[i - 1, j - 2] == 0)
                {
                    AddStep(i, j, i - 1, j - 2, 100 + GetTypeFigure(map[i - 1, j - 2]));
                }
            }
            if (IsInsideBorders(i + 1, j - 2))
            {
                if (map[i + 1, j - 2] == 0)
                {
                    AddStep(i, j, i + 1, j - 2, 100 + GetTypeFigure(map[i + 1, j - 2]));
                }
            }
        }

        private void BotCheckStepTower(int i, int j)
        {
            int jj = j + 1;
            while (IsInsideBorders(i, jj))          // вправо
            {
                if (map[i, jj] == 0)
                {
                    AddStep(i, j, i, jj, 100 + GetTypeFigure(map[i, jj]));
                }
                else { break; }
                jj++;
            }
            jj = j - 1;
            while (IsInsideBorders(i, jj))          // влево
            {
                if (map[i, jj] == 0)
                {
                    AddStep(i, j, i, jj, 100 + GetTypeFigure(map[i, jj]));
                }
                else { break; }
                jj--;
            }

            int ii = i + 1;
            while (IsInsideBorders(ii, j))          // вниз
            {
                if (map[ii, j] == 0)
                {
                    AddStep(i, j, ii, j, 100 + GetTypeFigure(map[ii, j]));
                }
                else { break; }
                ii++;
            }

            ii = i - 1;
            while (IsInsideBorders(ii, j))          // вверх
            {
                if (map[ii, j] == 0)
                {
                    AddStep(i, j, ii, j, 100 + GetTypeFigure(map[ii, j]));
                }
                else { break; }
                ii--;
            }
        }

        private void BotCheckStepPawn(int i, int j)
        {
            if(IsInsideBorders(i + 1, j))
            {
                if(map[i + 1, j] > 20 && map[i + 1, j] < 23)
                {
                    return;
                }
            }

            if (i > 3)
            {
                if (IsInsideBorders(i - 1, j))
                {
                    if (map[i - 1, j] == 0)
                    {
                        AddStep(i, j, i - 1, j, 126);
                        if (i - 2 > 3)
                        {
                            if (IsInsideBorders(i - 2, j))
                            {
                                if (map[i - 2, j] == 0)
                                {
                                    AddStep(i, j, i - 2, j, 126);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (IsInsideBorders(i - 1, j))
                {
                    if (map[i - 1, j] == 0)
                    {
                        AddStep(i, j, i - 1, j, 110);
                    }
                }
            }
            
        }

        private void BotCheckEat(int i, int j)
        {
            switch (map[i, j])
            {
                case 21:
                    BotCheckEatKing(i, j);
                    break;
                case 22:
                    BotCheckEatQueen(i, j);
                    break;
                case 23:
                    BotCheckEatElephant(i, j);
                    break;
                case 24:
                    BotCheckEatHorse(i, j);
                    break;
                case 25:
                    BotCheckEatTower(i, j);
                    break;
                case 26:
                    BotCheckEatPawn(i, j);
                    break;
            }
        }

        private void BotCheckEatKing(int i, int j)
        {
            if (IsInsideBorders(i + 1, j))
            {
                if (GetColorFigure(map[i + 1, j]) == 1)
                {
                    if (GetColorFigure(map[i + 1, j]) == 1)
                    {
                        AddStep(i, j, i + 1, j, GetTypeFigure(map[i + 1, j]));
                    }
                }
            }

            if (IsInsideBorders(i - 1, j))
            {
                if (GetColorFigure(map[i - 1, j]) == 1)
                {
                    AddStep(i, j, i - 1, j, GetTypeFigure(map[i - 1, j]));
                }
            }

            if (IsInsideBorders(i, j + 1))
            {
                if (GetColorFigure(map[i, j + 1]) == 1)
                {
                    AddStep(i, j, i, j + 1, GetTypeFigure(map[i, j + 1]));
                }
            }

            if (IsInsideBorders(i, j - 1))
            {
                if (GetColorFigure(map[i, j - 1]) == 1)
                {
                    AddStep(i, j, i, j - 1, GetTypeFigure(map[i, j - 1]));
                }
            }

            if (IsInsideBorders(i + 1, j + 1))
            {
                if (GetColorFigure(map[i + 1, j + 1]) == 1)
                {
                    AddStep(i, j, i + 1, j + 1, GetTypeFigure(map[i + 1, j + 1]));
                }
            }

            if (IsInsideBorders(i - 1, j + 1))
            {
                if (GetColorFigure(map[i - 1, j + 1]) == 1)
                {
                    AddStep(i, j, i - 1, j + 1, GetTypeFigure(map[i - 1, j + 1]));
                }
            }

            if (IsInsideBorders(i + 1, j - 1))
            {
                if (GetColorFigure(map[i + 1, j - 1]) == 1)
                {
                    AddStep(i, j, i + 1, j - 1, GetTypeFigure(map[i + 1, j - 1]));
                }
            }

            if (IsInsideBorders(i - 1, j - 1))
            {
                if (GetColorFigure(map[i - 1, j - 1]) == 1)
                {
                    AddStep(i, j, i - 1, j - 1, GetTypeFigure(map[i - 1, j - 1]));
                }
            }
        }

        private void BotCheckEatQueen(int i, int j)
        {
            BotCheckEatElephant(i, j);
            BotCheckEatTower(i, j);
        }

        private void BotCheckEatElephant(int i, int j)
        {
            int ii = i - 1;
            int jj = j - 1;
            while (IsInsideBorders(ii, jj))          // вправо
            {
                if (GetColorFigure(map[ii, jj]) == 2) { break; }
                if (GetColorFigure(map[ii, jj]) == 1)
                {
                    AddStep(i, j, ii, jj, GetTypeFigure(map[ii, jj]));
                    break;
                }
                ii--; jj--;
            }

            ii = i + 1;
            jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вниз-влево
            {
                if (GetColorFigure(map[ii, jj]) == 2) { break; }
                if (GetColorFigure(map[ii, jj]) == 1)
                {
                    AddStep(i, j, ii, jj, GetTypeFigure(map[ii, jj]));
                    break;
                }
                ii++; jj--;
            }

            ii = i - 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вверх-вправо
            {
                if (GetColorFigure(map[ii, jj]) == 2) { break; }
                if (GetColorFigure(map[ii, jj]) == 1)
                {
                    AddStep(i, j, ii, jj, GetTypeFigure(map[ii, jj]));
                    break;
                }
                ii--; jj++;
            }

            ii = i + 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вниз-вправо
            {
                if (GetColorFigure(map[ii, jj]) == 2) { break; }
                if (GetColorFigure(map[ii, jj]) == 1)
                {
                    AddStep(i, j, ii, jj, GetTypeFigure(map[ii, jj]));
                    break;
                }
                ii++; jj++;
            }
        }
     
        private void BotCheckEatHorse(int i, int j)
        {
            if (IsInsideBorders(i - 2, j + 1))
            {
                if (GetColorFigure(map[i - 2, j + 1]) == 1)
                {
                    AddStep(i, j, i - 2, j + 1, GetTypeFigure(map[i - 2, j + 1]));
                }
            }
            if (IsInsideBorders(i - 2, j - 1))
            {
                if (GetColorFigure(map[i - 2, j - 1]) == 1)
                {
                    AddStep(i, j, i - 2, j - 1, GetTypeFigure(map[i - 2, j - 1]));
                }
            }
            if (IsInsideBorders(i + 2, j + 1))
            {
                if (GetColorFigure(map[i + 2, j + 1]) == 1)
                {
                    AddStep(i, j, i + 2, j + 1, GetTypeFigure(map[i + 2, j + 1]));
                }
            }
            if (IsInsideBorders(i + 2, j - 1))
            {
                if (GetColorFigure(map[i + 2, j - 1]) == 1)
                {
                    AddStep(i, j, i + 2, j - 1, GetTypeFigure(map[i + 2, j - 1]));
                }
            }
            if (IsInsideBorders(i - 1, j + 2))
            {
                if (GetColorFigure(map[i - 1, j + 2]) == 1)
                {
                    AddStep(i, j, i - 1, j + 2, GetTypeFigure(map[i - 1, j + 2]));
                }
            }
            if (IsInsideBorders(i + 1, j + 2))
            {
                if (GetColorFigure(map[i - 1, j + 2]) == 1)
                {
                    AddStep(i, j, i + 1, j + 2, GetTypeFigure(map[i + 1, j + 2]));
                }
            }
            if (IsInsideBorders(i - 1, j - 2))
            {
                if (GetColorFigure(map[i - 1, j - 2]) == 1)
                {
                    AddStep(i, j, i - 1, j - 2, GetTypeFigure(map[i - 1, j - 2]));
                }
            }
            if (IsInsideBorders(i + 1, j - 2))
            {
                if (GetColorFigure(map[i + 1, j - 2]) == 1)
                {
                    AddStep(i, j, i + 1, j - 2, GetTypeFigure(map[i + 1, j - 2]));
                }
            }
        }

        private void BotCheckEatTower(int i, int j)
        {
            int jj = j + 1;
            while (IsInsideBorders(i, jj))          // вправо
            {
                if (GetColorFigure(map[i, jj]) == 2) { break; }
                if (GetColorFigure(map[i, jj]) == 1)
                {
                    AddStep(i, j, i, jj, GetTypeFigure(map[i, jj]));
                    break;
                }
                jj++;
            }
            jj = j - 1;
            while (IsInsideBorders(i, jj))          // влево
            {
                if(GetColorFigure(map[i, jj]) == 2) { break; }
                if (GetColorFigure(map[i, jj]) == 1)
                {
                    AddStep(i, j, i, jj, GetTypeFigure(map[i, jj]));
                    break;
                }
                jj--;
            }

            int ii = i + 1;
            while (IsInsideBorders(ii, j))          // вниз
            {
                if (GetColorFigure(map[ii, j]) == 2) { break; }
                if (GetColorFigure(map[ii, j]) == 1)
                {
                    AddStep(i, j, ii, j, GetTypeFigure(map[ii, j]));
                    break;
                }
                ii++;
            }

            ii = i - 1;
            while (IsInsideBorders(ii, j))          // вверх
            {
                if (GetColorFigure(map[ii, j]) == 2) { break; }
                if (GetColorFigure(map[ii, j]) == 1)
                {
                    AddStep(i, j, ii, j, GetTypeFigure(map[ii, j]));
                    break;
                }
                ii--;
            }
        }

        private void BotCheckEatPawn(int i, int j)
        {
            if(IsInsideBorders(i - 1, j - 1))
            {
                if(GetColorFigure(map[i - 1, j - 1]) == 1)
                {
                    AddStep(i, j, i - 1, j - 1, GetTypeFigure(map[i - 1, j - 1]));
                }
            }
            if (IsInsideBorders(i - 1, j + 1))
            {
                if (GetColorFigure(map[i - 1, j + 1]) == 1)
                {
                    AddStep(i, j, i - 1, j + 1, GetTypeFigure(map[i - 1, j + 1]));
                }
            }
        }

        private void FigureClickOnline(object sender, EventArgs e)
        {
            if(currentPlayer == 0) { currentPlayer = 1; }

            pressedButton = sender as Button;

            if (GetColorFigure(CheckMap(ConvertNameI(pressedButton), ConvertNameY(pressedButton))) != 0 &&
                GetColorFigure(CheckMap(ConvertNameI(pressedButton), ConvertNameY(pressedButton))) == currentPlayer)
            {
                // Очищаем поле после второго нажатие на ту же фигуру
                if (pressedButton.BackColor == Color.Red)
                {
                    pressedButton.BackColor = GetPrevButtonColor(pressedButton);
                    RefreshColorMap();
                    ActivateAllButtons();
                    isMoving = false;
                    return;
                }
                RefreshColorMap();
                DeactivateAllButtons();
                pressedButton.BackColor = Color.Red;        // Выделяем нажатую кнопку красным
                pressedButton.Enabled = true;
                ShowSteps(ConvertNameI(pressedButton), ConvertNameY(pressedButton));

                if (isMoving)
                {
                    RefreshColorMap();
                    pressedButton.BackColor = GetPrevButtonColor(pressedButton);
                    ShowSteps(ConvertNameI(pressedButton), ConvertNameY(pressedButton));    // Показываем куда можем сходить
                    isMoving = false;

                }
                else
                {
                    isMoving = true;
                }
            }
            else
            {
                if(isMoving)
                {
                    if(pressedButton.BackColor == Color.Red)
                    {
                        pressedButton.BackColor = GetPrevButtonColor(pressedButton);
                        
                    }
                    if(pressedButton.BackColor == Color.Yellow)
                    {
                        map[ConvertNameI(pressedButton), ConvertNameY(pressedButton)] = map[ConvertNameI(prevButton), ConvertNameY(prevButton)];
                        map[ConvertNameI(prevButton), ConvertNameY(prevButton)] = 0;
                        pressedButton.Image = prevButton.Image;
                        prevButton.Image = null;
                        prevButton.BackColor = Color.White;

                        isMoving = false;
                        CloseSteps();
                        DeactivateAllButtons();
                        SendMessage(GetMap() + currentPlayer);
                        CheckWin();
                    }
                }
            }
            prevButton = pressedButton;
        }

        private void ShowSteps(int i, int j)
        {
            // false - black , true - white
            switch (map[i, j])
            {
                case 11:
                    ShowStepsKing(i, j, 2);
                    break;
                case 12:
                    ShowStepsQueen(i, j, 2);
                    break;
                case 13:
                    ShowStepsElephant(i, j, 2);
                    break;
                case 14:
                    ShowStepsHorse(i, j, 2);
                    break;
                case 15:
                    ShowStepsTower(i, j, 2);
                    break;
                case 16:
                    ShowEatStepsPawn(i, j, true);
                    ShowStepsPawn(i, j, true);
                    break;

                case 21:
                    ShowStepsKing(i, j, 1);
                    break;
                case 22:
                    ShowStepsQueen(i, j, 1);
                    break;
                case 23:
                    ShowStepsElephant(i, j, 1);
                    break;
                case 24:
                    ShowStepsHorse(i, j, 1);
                    break;
                case 25:
                    ShowStepsTower(i, j, 1);
                    break;
                case 26:
                    ShowEatStepsPawn(i, j, false);
                    ShowStepsPawn(i, j, false);
                    break;
            }
        }

        private void ShowStepsKing(int i, int j, int Figure)
        {
            if (IsInsideBorders(i + 1, j))
            {
                if (map[i + 1, j] == 0 || GetColorFigure(map[i + 1, j]) == Figure)
                {
                    buttons[i + 1, j].Enabled = true;
                    buttons[i + 1, j].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i - 1, j))
            {
                if (map[i - 1, j] == 0 || GetColorFigure(map[i - 1, j]) == Figure)
                {
                    buttons[i - 1, j].Enabled = true;
                    buttons[i - 1, j].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i, j + 1))
            {
                if (map[i, j + 1] == 0 || GetColorFigure(map[i, j + 1]) == Figure)
                {
                    buttons[i, j + 1].Enabled = true;
                    buttons[i, j + 1].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i, j - 1))
            {
                if (map[i, j - 1] == 0 || GetColorFigure(map[i, j - 1]) == Figure)
                {
                    buttons[i, j - 1].Enabled = true;
                    buttons[i, j - 1].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i + 1, j + 1))
            {
                if (map[i + 1, j + 1] == 0 || GetColorFigure(map[i + 1, j + 1]) == Figure)
                {
                    buttons[i + 1, j + 1].Enabled = true;
                    buttons[i + 1, j + 1].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i - 1, j + 1))
            {
                if (map[i - 1, j + 1] == 0 || GetColorFigure(map[i - 1, j + 1]) == Figure)
                {
                    buttons[i - 1, j + 1].Enabled = true;
                    buttons[i - 1, j + 1].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i + 1, j - 1))
            {
                if (map[i + 1, j - 1] == 0 || GetColorFigure(map[i + 1, j - 1]) == Figure)
                {
                    buttons[i + 1, j - 1].Enabled = true;
                    buttons[i + 1, j - 1].BackColor = Color.Yellow;
                }
            }

            if (IsInsideBorders(i - 1, j - 1))
            {
                if (map[i - 1, j - 1] == 0 || GetColorFigure(map[i - 1, j - 1]) == Figure)
                {
                    buttons[i - 1, j - 1].Enabled = true;
                    buttons[i - 1, j - 1].BackColor = Color.Yellow;
                }
            }          
        }

        private void ShowStepsQueen(int i, int j, int Figure)
        {
            ShowStepsElephant(i, j, Figure);
            ShowStepsTower(i, j, Figure);
        }

        private void ShowStepsElephant(int i, int j, int Figure)
        {
            int ii = i - 1;
            int jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вверх-влево
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                    if(GetColorFigure(map[ii, jj]) == Figure) { break; }
                }
                else { break; }
                ii--; jj--;
            }

            ii = i + 1;
            jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вниз-влево
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                    if (GetColorFigure(map[ii, jj]) == Figure) { break; }
                }
                else { break; }
                ii++; jj--;
            }

            ii = i - 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вверх-вправо
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                    if (GetColorFigure(map[ii, jj]) == Figure) { break; }
                }
                else { break; }
                ii--; jj++;
            }

            ii = i + 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вниз-вправо
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                    if (GetColorFigure(map[ii, jj]) == Figure) { break; }
                }
                else { break; }
                ii++; jj++;
            }
        }

        private void ShowStepsHorse(int i, int j, int Figure)
        {
            int ii = i - 2;
            int jj = j + 1;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                }
            }
            ii = i - 2;
            jj = j - 1;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                }
            }
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow; 
                }
            }
            ii = i + 2;
            jj = j - 1;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                }
            }
            ii = i + 2;
            jj = j + 1;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                }
            }
            ii = i - 1;
            jj = j + 2;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;
                }
            }
            ii = i + 1;
            jj = j + 2;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow; 
                }
            }
            ii = i - 1;
            jj = j - 2;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;    
                }
            }
            ii = i + 1;
            jj = j - 2;
            if (IsInsideBorders(ii, jj))
            {
                if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == Figure)
                {
                    buttons[ii, jj].Enabled = true;
                    buttons[ii, jj].BackColor = Color.Yellow;    
                }
            }
        }

        private void ShowStepsTower(int i, int j, int Figure)
        {

            int jj = j + 1;
            while (IsInsideBorders(i, jj))          // вправо
            {
                if (map[i, jj] == 0 || GetColorFigure(map[i, jj]) == Figure)
                {
                    buttons[i, jj].Enabled = true;
                    buttons[i, jj].BackColor = Color.Yellow;
                    if (GetColorFigure(map[i, jj]) == Figure) { break; }
                }
                else { break; }
                jj++;
            }
            jj = j - 1;
            while (IsInsideBorders(i, jj))          // влево
            {
                if (map[i, jj] == 0 || GetColorFigure(map[i, jj]) == Figure)
                {
                    buttons[i, jj].Enabled = true;
                    buttons[i, jj].BackColor = Color.Yellow;
                    if (GetColorFigure(map[i, jj]) == Figure) { break; }
                }
                else { break; }
                jj--;
            }

            int ii = i + 1;
            while (IsInsideBorders(ii, j))          // вниз
            {
                if (map[ii, j] == 0 || GetColorFigure(map[ii, j]) == Figure)
                {
                    buttons[ii, j].Enabled = true;
                    buttons[ii, j].BackColor = Color.Yellow;
                    if (GetColorFigure(map[ii, j]) == Figure) { break; }
                }
                else { break; }
                ii++;
            }

            ii = i - 1;
            while (IsInsideBorders(ii, j))          // вверх
            {
                if (map[ii, j] == 0 || GetColorFigure(map[ii, j]) == Figure)
                {
                    buttons[ii, j].Enabled = true;
                    buttons[ii, j].BackColor = Color.Yellow;
                    if (GetColorFigure(map[ii, j]) == Figure) { break; }
                }
                else { break; }
                ii--;
            }
        }

        private void ShowEatStepsPawn(int i, int j, bool IsColor)
        {
            if(IsColor)
            {
                int ii = i + 1;
                int jj = j - 1;
                if (IsInsideBorders(ii, jj))
                {
                    if (GetColorFigure(map[ii, jj]) == 2)
                    {
                        buttons[ii, jj].Enabled = true;
                        buttons[ii, jj].BackColor = Color.Yellow;
                    }
                }

                ii = i + 1;
                jj = j + 1;
                if (IsInsideBorders(ii, jj))
                {
                    if (GetColorFigure(map[ii, jj]) == 2)
                    {
                        buttons[ii, jj].Enabled = true;
                        buttons[ii, jj].BackColor = Color.Yellow;
                    }
                }
            }
            else
            {
                int ii = i - 1;
                int jj = j - 1;
                if (IsInsideBorders(ii, jj))
                {
                    if (GetColorFigure(map[ii, jj]) == 2)
                    {
                        buttons[ii, jj].Enabled = true;
                        buttons[ii, jj].BackColor = Color.Yellow;
                    }
                }

                ii = i - 1;
                jj = j + 1;
                if (IsInsideBorders(ii, jj))
                {
                    if (GetColorFigure(map[ii, jj]) == 2)
                    {
                        buttons[ii, jj].Enabled = true;
                        buttons[ii, jj].BackColor = Color.Yellow;
                    }
                }
            }
        }

        private void ShowStepsPawn(int i, int j, bool IsColor)
        {
            if(IsColor)
            {
                // На своей половине
                if (i < 3)
                {
                    int ii = i + 1;
                    int iii = i + 2;
                    if (IsInsideBorders(ii, j))
                    {
                        if (map[ii, j] == 0)
                        {
                            buttons[ii, j].Enabled = true;
                            buttons[ii, j].BackColor = Color.Yellow;
                            if (IsInsideBorders(iii, j))
                            {
                                if (iii > 3) { return; }
                                if (map[iii, j] == 0)
                                {
                                    buttons[iii, j].Enabled = true;
                                    buttons[iii, j].BackColor = Color.Yellow;
                                }
                            }
                        }
                    }
                }
                else
                {
                    int ii = i + 1;
                    if (IsInsideBorders(ii, j))
                    {
                        if (map[ii, j] == 0)
                        {
                            buttons[ii, j].Enabled = true;
                            buttons[ii, j].BackColor = Color.Yellow;
                        }
                    }
                }
            }
            else
            {
                int ii = i - 1;
                int jj = j - 1;
                if (IsInsideBorders(ii, jj))
                {
                    if (GetColorFigure(map[ii, jj]) == 1)
                    {
                        buttons[ii, jj].Enabled = true;
                        buttons[ii, jj].BackColor = Color.Yellow;
                    }
                }

                ii = i - 1;
                jj = j + 1;
                if (IsInsideBorders(ii, jj))
                {
                    if (GetColorFigure(map[ii, jj]) == 1)
                    {
                        buttons[ii, jj].Enabled = true;
                        buttons[ii, jj].BackColor = Color.Yellow;
                    }
                }

                // Чёрные пешки
                if (i > 3)
                {
                    ii = i - 1;
                    int iii = i - 2;
                    if (IsInsideBorders(ii, j))
                    {
                        if (map[ii, j] == 0)
                        {
                            buttons[ii, j].Enabled = true;
                            buttons[ii, j].BackColor = Color.Yellow;
                            if (IsInsideBorders(iii, j))
                            {
                                if (iii < 3) { return; }
                                if (map[iii, j] == 0)
                                {
                                    buttons[iii, j].Enabled = true;
                                    buttons[iii, j].BackColor = Color.Yellow;
                                }
                            }
                        }
                    }
                }
                else
                {
                    ii = i - 1;
                    //jj;
                    if (IsInsideBorders(ii, j))
                    {
                        if (map[ii, j] == 0)
                        {
                            buttons[ii, j].Enabled = true;
                            buttons[ii, j].BackColor = Color.Yellow;
                        }
                    }
                }
            }
        }

        private void BotChoosingImportingMove(int[,] _map)
        {
            for(int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (_map[i, j] < 20)
                    {
                        CheckStep(i, j, _map[i, j]);
                    }
                }
            }
            //MessageBox.Show(stepsPlayer.Count.ToString());
            for (int i = 0; i < stepsPlayer.Count; i++)
            {
                if (stepsPlayer[i][4] != 0 && stepsPlayer[i][4] != 1)
                {
                    //MessageBox.Show(stepsPlayer[i][0] + " " + stepsPlayer[i][1] + " " + stepsPlayer[i][2] + " " + stepsPlayer[i][3] + " " + stepsPlayer[i][4]);
                }
            }
        }

        private void CheckStep(int i, int j, int Figure)
        {
            switch(Figure)
            {
                case 11:
                    CheckAllStepsKing(i, j, Figure);
                    break;
                case 12:
                    CheckAllStepsQueen(i, j, Figure);
                    break;
                case 13:
                    CheckAllStepsElephant(i, j, Figure);
                    break;
                case 14:
                    CheckAllStepsHorse(i, j, Figure);
                    break;
                case 15:
                    CheckAllStepsTower(i, j, Figure);
                    break;
                case 16:
                    CheckAllStepsPawn(i, j);
                    break;
            }
        }

        private void CheckAllStepsKing(int i, int j, int Figure)
        {
            if (IsInsideBorders(i + 1, j))
            {
                if (map[i + 1, j] == 0 || GetColorFigure(map[i + 1, j]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i + 1, j, GetTypeFigure(map[i + 1, j]) });
                }
            }

            if (IsInsideBorders(i - 1, j))
            {
                if (map[i - 1, j] == 0 || GetColorFigure(map[i - 1, j]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i - 1, j, GetTypeFigure(map[i - 1, j]) });
                }
            }

            if (IsInsideBorders(i, j + 1))
            {
                if (map[i, j + 1] == 0 || GetColorFigure(map[i, j + 1]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i, j + 1, GetTypeFigure(map[i, j + 1]) });
                }
            }

            if (IsInsideBorders(i, j - 1))
            {
                if (map[i, j - 1] == 0 || GetColorFigure(map[i, j - 1]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i, j - 1, GetTypeFigure(map[i, j - 1]) });
                }
            }

            if (IsInsideBorders(i + 1, j + 1))
            {
                if (map[i + 1, j + 1] == 0 || GetColorFigure(map[i + 1, j + 1]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i + 1, j + 1, GetTypeFigure(map[i + 1, j + 1]) });
                }
            }

            if (IsInsideBorders(i - 1, j + 1))
            {
                if (map[i - 1, j + 1] == 0 || GetColorFigure(map[i - 1, j + 1]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i - 1, j + 1, GetTypeFigure(map[i - 1, j + 1]) });
                }
            }

            if (IsInsideBorders(i + 1, j - 1))
            {
                if (map[i + 1, j - 1] == 0 || GetColorFigure(map[i + 1, j - 1]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i + 1, j - 1, GetTypeFigure(map[i + 1, j - 1]) });
                }
            }

            if (IsInsideBorders(i - 1, j - 1))
            {
                if (map[i - 1, j - 1] == 0 || GetColorFigure(map[i - 1, j - 1]) == Figure)
                {
                    stepsPlayer.Add(new int[5] { i, j, i - 1, j - 1, GetTypeFigure(map[i - 1, j - 1]) });
                }
            }
        }

        private void CheckAllStepsQueen(int i, int j, int Figure)
        {
            CheckAllStepsElephant(i, j, Figure);
            CheckAllStepsTower(i, j, Figure);
        }

        private void CheckAllStepsElephant(int i, int j, int Figure)
        {
            int ii = i - 1;
            int jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вверх-влево
            {
                if (GetColorFigure(map[ii, jj]) == 2 || map[ii, jj] == 0)
                {
                    stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    if (GetColorFigure(map[ii, jj]) == 2) { break; }
                }
                else { break; }
                ii--; jj--;
            }

           

            ii = i + 1;
            jj = j - 1;
            while (IsInsideBorders(ii, jj))          // Вниз-влево
            {
                if (GetColorFigure(map[ii, jj]) == 2 || map[ii, jj] == 0)
                {
                    stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    if (GetColorFigure(map[ii, jj]) == 2) { break; }
                }
                else { break; }
                ii++; jj--;
            }

            ii = i - 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вверх-вправо
            {
                if (GetColorFigure(map[ii, jj]) == 2 || map[ii, jj] == 0)
                {
                    stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    if (GetColorFigure(map[ii, jj]) == 2) { break; }
                }
                else { break; }
                ii--; jj++;
            }
           
            ii = i + 1;
            jj = j + 1;
            while (IsInsideBorders(ii, jj))          // Вниз-вправо
            {
                if (GetColorFigure(map[ii, jj]) == 2 || map[ii, jj] == 0)
                {
                    stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    if (GetColorFigure(map[ii, jj]) == 2) { break; }
                }
                else { break; }
                ii++; jj++;
            }
        }

        private void CheckAllStepsHorse(int i, int j, int Figure)
        {
            int ii = i - 2;
            int jj = j + 1;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i - 2;
            jj = j - 1;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i + 2;
            jj = j - 1;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i + 2;
            jj = j + 1;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i - 1;
            jj = j + 2;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i + 1;
            jj = j + 2;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i - 1;
            jj = j - 2;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
            ii = i + 1;
            jj = j - 2;
            if (IsInsideBorders(ii, jj))
            {
                if (GetColorFigure(map[ii, jj]) != 1)
                {
                    if (map[ii, jj] == 0 || GetColorFigure(map[ii, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                    }
                }
            }
        }

        private void CheckAllStepsTower(int i, int j, int Figure)
        {

            int jj = j + 1;
            while (IsInsideBorders(i, jj))          // вправо
            {
                if (GetColorFigure(map[i, jj]) != 1)
                {
                    if (map[i, jj] == 0 || GetColorFigure(map[i, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, i, jj, GetTypeFigure(map[i, jj]) });
                    }
                    if (GetColorFigure(map[i, jj]) == 2) { break; }
                }
                else { break; }
                jj++;
            }
            jj = j - 1;
            while (IsInsideBorders(i, jj))          // влево
            {
                if (GetColorFigure(map[i, jj]) != 1)
                {
                    if (map[i, jj] == 0 || GetColorFigure(map[i, jj]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, i, jj, GetTypeFigure(map[i, jj]) });
                    }
                    if (GetColorFigure(map[i, jj]) == 2) { break; }
                }
                else { break; }
                jj--;
            }

            int ii = i + 1;
            while (IsInsideBorders(ii, j))          // вниз
            {
                if (GetColorFigure(map[ii, j]) != 1)
                {
                    if (map[ii, j] == 0 || GetColorFigure(map[ii, j]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, j, GetTypeFigure(map[ii, j]) });
                    }
                    if (GetColorFigure(map[ii, j]) == 2) { break; }
                }
                else { break; }
                ii++;
            }

            ii = i - 1;
            while (IsInsideBorders(ii, j))          // вверх
            {
                if (GetColorFigure(map[ii, j]) != 1)
                {
                    if (map[ii, j] == 0 || GetColorFigure(map[ii, j]) == 2)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, j, GetTypeFigure(map[ii, j]) });
                    }
                    if (GetColorFigure(map[ii, j]) == 2) { break; }
                }
                else { break; }
                ii--;
            }
        }

        private void CheckAllStepsPawn(int i, int j)
        {
            int ii;
            int iii;
            int jj;
            // На своей половине
            if (i < 3)
            {
                ii = i + 1;
                iii = i + 2;
                if (IsInsideBorders(ii, j))
                {
                    if (map[ii, j] == 0)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, j, GetTypeFigure(map[ii, j]) });
                        if (IsInsideBorders(iii, j))
                        {
                            if (iii > 3) { return; }
                            if (map[iii, j] == 0)
                            {
                                stepsPlayer.Add(new int[5] { i, j, iii, j, GetTypeFigure(map[iii, j]) });
                            }
                        }
                    }
                }
            }
            else
            {
                ii = i + 1;
                if (IsInsideBorders(ii, j))
                {
                    if (map[ii, j] == 0)
                    {
                        stepsPlayer.Add(new int[5] { i, j, ii, j, GetTypeFigure(map[ii, j]) });
                    }
                }
            }

            ii = i + 1;
            jj = j - 1;
            if (IsInsideBorders(ii, jj))
            {
                if (GetTypeFigure(map[ii, jj]) == 2)
                {
                    stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                }
            }

            ii = i + 1;
            jj = j + 1;
            if (IsInsideBorders(ii, jj))
            {
                if (GetTypeFigure(map[ii, jj]) == 2)
                {
                    stepsPlayer.Add(new int[5] { i, j, ii, jj, GetTypeFigure(map[ii, jj]) });
                }
            }

        }

        private void CheckWin()
        {
            bool IsLiveWhite = false;
            bool IsLiveBlack = false;

            for(int i = 0; i < mapSize; i++)
            {
                for(int j = 0; j < mapSize; j++)
                {
                    if(map[i,j] == 11)
                    {
                        IsLiveWhite = true;
                    }
                    if(map[i,j] == 21)
                    {
                        IsLiveBlack = true;
                    }
                }
            }
            if(IsLiveBlack && IsLiveWhite) { return; }
            else 
            {
                if (!IsLiveBlack)
                {
                    if (currentPlayer == 1)
                    {
                        EndGame = new end_of_game.end_of_game("Win", this);
                        EndGame.Owner = this;
                        EndGame.Show();
                    }
                    else
                    {
                        EndGame = new end_of_game.end_of_game("Fail", this);
                        EndGame.Owner = this;
                        EndGame.Show();
                    }
                }
                if (!IsLiveWhite)
                {
                    if (currentPlayer == 2)
                    {
                        EndGame = new end_of_game.end_of_game("Win", this);
                        EndGame.Owner = this;
                        EndGame.Show();
                    }
                    else
                    {
                        EndGame = new end_of_game.end_of_game("Fail", this);
                        EndGame.Owner = this;
                        EndGame.Show();
                    }
                }
            }
        }

        private void CloseSteps()
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    buttons[i, j].BackColor = GetPrevButtonColor(buttons[i, j]);
                }
            }
        }

        private Image GetImage(int type) // Белые:  0 - пустота, 11 - король, 12 - королева, 13 - слон, 14 - конь, 15 - ладья, 16 - пешка
        {
            switch(type)
            {
                // White
                case 11:
                    return whiteKing;
                case 12:
                    return whiteQueen;
                case 13:
                    return whiteElephant;
                case 14:
                    return whiteHorse;
                case 15:
                    return whiteTower;
                case 16:
                    return whitePawn;

                // Black
                case 21:
                    return blackKing;
                case 22:
                    return blackQueen;
                case 23:
                    return blackElephant;
                case 24:
                    return blackHorse;
                case 25:
                    return blackTower;
                case 26:
                    return blackPawn;
                default:
                    return null;
            }
        }

        // Цвет выбранной фигуры
        private int GetColorFigure(int val)
        {
            return val / 10;
        }

        // Тип выбранной фигуры
        private int GetTypeFigure(int val)
        {
            return val % 10;
        }

        // Возвращает фигуру находящуюся по переданным координатам
        private int CheckMap(int i, int j)
        {
            if (i < mapSize && j < mapSize)
            {
                return map[i, j];
            }
            else { return 0; }
        }

        private int[] CheckShah()
        {
            for(int i = 0; i < stepsPlayer.Count; i++)
            {
                if(stepsPlayer[i][4] == 1)
                {
                    MessageBox.Show("Чёрным шах!", "Шах", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return new int[5] { stepsPlayer[i][0], stepsPlayer[i][1], stepsPlayer[i][2], stepsPlayer[i][3], stepsPlayer[i][4] } ;
                }
            }
            return new int[] { -1, -1, -1, -1, -1 };
        }

        // Получить позицию кнопки по I
        private int ConvertNameI(Button b)
        {
            string sym = b.Name;
            return Convert.ToInt32(sym[6]) - 48;
        }

        // Получить позицию кнопки по Y
        private int ConvertNameY(Button b)
        {
            string sym = b.Name;
            return Convert.ToInt32(sym[8]) - 48;
        }

        private Color GetPrevButtonColor(Button prevButton)
        {
            if ((prevButton.Location.Y / cellSize % 2) != 0)
            {
                if ((prevButton.Location.X / cellSize % 2) == 0)
                {
                    return Color.Gray;
                }
            }
            if ((prevButton.Location.Y / cellSize) % 2 == 0)
            {
                if ((prevButton.Location.X / cellSize) % 2 != 0)
                {
                    return Color.Gray;
                }
            }
            return Color.White;
        }

        private void RefreshColorMap()
        {
            int k = 0;
            for(int i = 0; i < mapSize; i++)
            {
                for(int j = 0; j < mapSize; j++)
                {
                    buttons[i, j].BackColor = colorMap[k];
                    k++;
                }
            }
        }

        public string GetMap()
        {
            string res = "";
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    res += map[i, j].ToString() + ",";
                }
            }
            return res;
        }

        private bool IsInsideBorders(int i, int j)
        {
            if (i >= mapSize || j >= mapSize || i < 0 || j < 0)
            {
                return false;   // Не нахоидтся
            }
            return true; // Находится
        }

        private void AddStep(int i, int j, int ii, int jj, int wt)
        {
            int[] xod = new int[5];

            xod[0] = i; xod[1] = j;
            xod[2] = ii; xod[3] = jj;
            xod[4] = wt;

            stepsBot.Add(xod);
        }

        // Пройтись по всем кнопкам и сделать их активными
        private void ActivateAllButtons()
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    buttons[i, j].Enabled = true;
                }
            }
        }

        // Пройтись по всем кнопкам и сделать их не активными
        private void DeactivateAllButtons()
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (buttons[i, j].BackColor != Color.Yellow)
                    {
                        buttons[i, j].Enabled = false;
                    }
                }
            }
        }

        private void ChangeAfterListen(string msg)
        {
            string[] razborMessage = new string[65];
            razborMessage = msg.Split(',');

            // Выполняем в отдельном потоке, так как будем менять нашу форму
            this.Invoke((MethodInvoker)delegate
            {
                int k = 0;
                // Переписываем все пришедшие координаты
                for (int i = 0; i < mapSize; i++)
                {
                    for (int j = 0; j < mapSize; j++)
                    {
                        int num = Convert.ToInt32(razborMessage[k]);
                        map[i, j] = num;    // Переписываем пришедшую карту на нашу

                        SetInputFigure(i, j, num);
                        k++;
                    }
                }
            });

            //  Присваиваем игроку его фигуру
            if (currentPlayer == 0)
            {
                if (Convert.ToInt32(razborMessage[64]) == 1)
                { currentPlayer = 2; }
            }
            ActivateAllButtons();
        }

        // 0 - пустота, 11 - король, 12 - королева, 13 - слон, 14 - конь, 15 - ладья, 16 - пешка
        private void SetInputFigure(int i, int j, int num)
        {
            switch(num)
            {
                case 11:
                    buttons[i, j].Image = whiteKing;
                    break;
                case 12:
                    buttons[i, j].Image = whiteQueen;
                    break;
                case 13:
                    buttons[i, j].Image = whiteElephant;
                    break;
                case 14:
                    buttons[i, j].Image = whiteHorse;
                    break;
                case 15:
                    buttons[i, j].Image = whiteTower;
                    break;
                case 16:
                    buttons[i, j].Image = whitePawn;
                    break;

                // Black
                case 21:
                    buttons[i, j].Image = blackKing;
                    break;
                case 22:
                    buttons[i, j].Image = blackQueen;
                    break;
                case 23:
                    buttons[i, j].Image = blackElephant;
                    break;
                case 24:
                    buttons[i, j].Image = blackHorse;
                    break;
                case 25:
                    buttons[i, j].Image = blackTower;
                    break;
                case 26:
                    buttons[i, j].Image = blackPawn;
                    break;
                
                case 0:
                    buttons[i, j].Image = null;
                    break;
            }
            buttons[i, j].BackColor = GetPrevButtonColor(buttons[i, j]);
        }

        // Подключение к серверу
        private void Server_Connect()
        {
            // Создаём клиент
            client = new TcpClient();
            try
            {
                client.Connect(host, port); //подключение клиента
                stream = client.GetStream(); // получаем поток

                while (true)
                {
                    byte[] data = new byte[64]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;

                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.UTF8.GetString(data, 0, bytes));

                    ID = builder.ToString();            // Присваиваем ID

                    break;
                }

                // запускаем новый поток для получения данных
                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                receiveThread.Start(); //старт потока

                // Каждые 10 секунд проверяем есть ли соединение с сервером!
                Thread listenConnection = new Thread(new ThreadStart(CheckConnection));
                listenConnection.Start();
            }
            catch (Exception ex)
            {
                // Вывод ошибки в консоль, скорее всего сервер не запущен, или случилась хрень
                Console.WriteLine(ex.Message);
                MessageBox.Show("Сервер не отвечает", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        // Отправить сообщение
        private void SendMessage(string message)
        {
            if ((message != ""))
            {
                byte[] buffer = new byte[1024];
                buffer = Encoding.UTF8.GetBytes(message);   // Делаем байт код в формате UTF-8(Это важно) и отправляем его на сервер
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        // Получение сообщений
        void ReceiveMessage()
        {
            // Бесконечно слушаем
            while (true)
            {
                try
                {
                    byte[] data = new byte[64]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    this.Invoke((MethodInvoker)delegate
                    {
                        ChangeAfterListen(builder.ToString());  // Вызываем функцию разбора пришедшего сообщения
                        ActivateAllButtons();                   // Активируем все кнопки 
                        CheckWin();                             // Проверяем нет ли победителя
                    });

                    Console.WriteLine(builder.ToString());      // Вывод полученного сообщения
                }
                catch
                {
                    Console.WriteLine("Подключение прервано!"); // Соединение было прервано (Игра выключена, игра крашнулась)
                    MessageBox.Show("Подключение разрвано, приложение закроется через 5 секунд", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Thread.Sleep(5000);
                    Disconnect();
                }
            }
        }

        // Проверка есть ли соединение с сервером, каждые 10 секунд
        private void CheckConnection()
        {
            while (true)
            {
                Thread.Sleep(10000);
                if (!client.Connected)
                {
                    MessageBox.Show("Соединение с сервером разорвано!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Отключение, закрывает подключение с сервером и убивает ВСЁ приложение
        void Disconnect()
        {
            if (stream != null)
                stream.Close();//отключение потока
            if (client != null)
                client.Close();//отключение клиента
            Environment.Exit(0); //завершение процесса
        }

        private void Chess_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (EndGame == null)
            {
                ListGames lg = new ListGames();

                if (currentPlayer != 0)
                {
                    DialogResult dialog = MessageBox.Show("Игра только началась. Закрыть окно?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialog == DialogResult.Yes)
                    {
                        sound.Stop();
                        lg.Show();
                    }
                    if (dialog == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    sound.Stop();
                    lg.Show();
                }
            }else { sound.Stop(); }
        }
    }
}
