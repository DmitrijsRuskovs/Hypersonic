using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Player
{
    static Cell[,] gridC;
    static int width;
    static int height;
    static int turn = 0;
   
    static List<Entity> BombList = new List<Entity>();
    static List<Entity> ItemList = new List<Entity>();
    static List<Entity> GamerList = new List<Entity>();
    static NewPlayer MyPlayer = new NewPlayer(0, 0, 1, 2);

    struct NewPlayer
    {        
        public NewPlayer(int x, int y, int bombs, int range)
        {
            coord.X = x;
            coord.Y = y;
            explosionRange = range;
            bombsLeft = bombs;
            canPutBombs = bombs > 0; 
            destination = new Coord(x, y);
            lastBomb = 0;
        }

        public Coord coord;
        public int explosionRange;
        public bool canPutBombs;
        public int bombsLeft;
        public Coord destination;   
        public int lastBomb;    
    }

    class Entity
    {
        public Entity(string line)
        {
            string[] inputs = line.Split(' ');
            entityType = int.Parse(inputs[0]);
            owner = int.Parse(inputs[1]);
            x = int.Parse(inputs[2]);
            y = int.Parse(inputs[3]);
            param1 = int.Parse(inputs[4]);
            param2 = int.Parse(inputs[5]);
        }

        public Entity(Entity clone)
        {
            entityType = clone.entityType;
            owner = clone.owner;
            x = clone.x;
            y = clone.y;
            param1 = clone.param1;
            param2 = clone.param2;
        }

        public Entity(int X, int Y, int Range, int Time)
        {
            entityType = 1;
            owner = 0;
            x = X;
            y = Y;
            param1 = Time;
            param2 = Range;
        }
       
        public int entityType;
        public int owner;
        public int x;
        public int y;              
        public int param1 {get; set;}
        public int param2 {get; set;} 
        public Coord coord() => new Coord(x,y);
    }

    struct Cell
    {
       public char type;
       public int access;
       public int safe;
       public int boxCount;
    }

    struct Coord
    {
        public Coord(int x, int y) { X = x; Y = y; }
        public bool Equals(Coord coord2) => this.X == coord2.X && this.Y == coord2.Y;       
        public int X;
        public int Y;
    }

    static bool UnderDirectFire(int X, int Y, int newX, int newY)
    {
        bool result =false;        
        foreach (Entity bomb in BombList)
        {           
            int range = bomb.param2;                                          
            if ((bomb.x == X && Math.Abs(bomb.y - Y) < range && NoObstaclesX(bomb.x, bomb.y, Y)) ||
                (bomb.y == Y && Math.Abs(bomb.x - X) < range && NoObstaclesY(bomb.y, bomb.x, X))) 
            {
                result = true;   
            }    
            if ((newX < Math.Max(X, bomb.x) && newX > Math.Min(X, bomb.x) && Y == newY && Y == bomb.y) ||
                (newY < Math.Max(Y, bomb.y) && newY > Math.Min(Y, bomb.y) && X == newX && X == bomb.x))
            {
                result = false;   
            } 

        }                   
        return result;
    }

    static bool UnderFire(int X, int Y)
    {
        bool result =false;        
        foreach (Entity bomb in BombList)
        {           
            int range = bomb.param2;                                    
            if ((bomb.x == X && Math.Abs(bomb.y - Y) < range && NoObstaclesX(bomb.x, bomb.y, Y)) ||
                (bomb.y == Y && Math.Abs(bomb.x - X) < range && NoObstaclesY(bomb.y, bomb.x, X))) 
            {
                result = true;   
            }                                   
        }                   
        return result;
    }

    static bool bombIsAdequateHere(int X, int Y)
    {
        bool result = false;
        if (BoxCountC(X, Y, MyPlayer.explosionRange) > 1)
        {
            result = true;
        } 
        else if (BoxCountC(X, Y, MyPlayer.explosionRange) > 0 && MyPlayer.bombsLeft > 1)
        {
            result = true;
        }
        else if (BoxCountC(X, Y, MyPlayer.explosionRange) > 0 && MyPlayer.bombsLeft > 0 && UnderFire(X, Y))
        {
            result = true;
        }
        return result;
    }

    static bool NoObstaclesX(int X, int Y1, int Y2)
    {
        bool result = true;
        string allowed = "X0123456789";
        for (int i = Math.Min(Y1, Y2) + 1; i < Math.Max(Y1, Y2); i++)
        {
           foreach (Entity item in ItemList) 
           {
               if (item.x == X && item.y == i)
               {
                   result = false;
               }
           }               
           if (allowed.Contains(gridC[X, i].type)) 
           {
               result = false;
           }
        }
        return result;
    }

    static bool NoObstaclesY(int Y, int X1, int X2)
    {
        bool result = true;
        string allowed = "X0123456789";
        for (int i = Math.Min(X1, X2) + 1; i < Math.Max(X1, X2); i++)
        {
            foreach (Entity item in ItemList) 
            {
                if (item.x == i && item.y == Y) 
                {
                    result = false;
                }
            }
            if (allowed.Contains(gridC[i, Y].type))
            {
                result = false;
            } 
        }
        return result;
    }

    static void OrganizeAccessLevels(int X, int Y)
    {
        
        for (int j = 0; j < height; j++)
        { 
            for (int i = 0; i < width; i++) 
            {
                gridC[i, j].access = 100;  
            }   
        }             
        gridC[X, Y].access = 0;  
        for (int s = 1; s <= 15; s++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++) 
                {
                    if (gridC[i, j].access == s-1)
                    {
                        if (i-1 >= 0)
                        {
                            if (gridC[i - 1, j].access == 100 && (gridC[i - 1, j].type == '.'))
                            {
                                gridC[i - 1, j].access = s;
                            }
                        }
                        if (j-1 >= 0)
                        {
                            if (gridC[i, j - 1].access == 100 && (gridC[i, j - 1].type == '.'))
                            {
                                gridC[i, j - 1].access = s;
                            }
                        }
                        if (i+1 < width)
                        {
                            if (gridC[i + 1, j].access == 100 && (gridC[i + 1, j].type == '.'))
                            {
                                gridC[i + 1, j].access = s;
                            }
                        }
                        if (j+1 < height)
                        {
                            if (gridC[i, j + 1].access == 100 && (gridC[i, j + 1].type == '.'))
                            {
                                gridC[i, j + 1].access = s;
                            }
                        }
                    }  
                }
            }
        }   
    }

    static void OrganizeSafetyLevels()
    {
        for (int count = 1; count < 4; count++)
        {
            for (int bomb = 0; bomb < BombList.Count(); bomb++)
            {
                int range1 = BombList[bomb].param2;                    
                int time1 = BombList[bomb].param1;
                int x1 = BombList[bomb].x;
                int y1 = BombList[bomb].y;
                for (int bomb2 = 0; bomb2 < BombList.Count(); bomb2++) 
                {
                    if (bomb2 != bomb)
                    {
                        int range2 = BombList[bomb2].param2;
                        int time2 = BombList[bomb2].param1;
                        int x2 = BombList[bomb2].x;
                        int y2 = BombList[bomb2].y;
                        if (time1 > time2)
                        {                   
                            if (x1 == x2 && Math.Abs(y2 - y1) < range2 && NoObstaclesX(x1, y1, y2))
                            { 
                                BombList[bomb].param1 = time2; 
                                time1 = time2; 
                            }
                            if (y1 == y2 && Math.Abs(x2 - x1) < range2 && NoObstaclesY(y1, x1, x2))
                            { 
                                BombList[bomb].param1 = time2; 
                                time1 = time2;
                            }                   
                        }                                      
                        if (time1 < time2)
                        {                   
                            if (x1 == x2 && Math.Abs(y2 - y1) < range1 && NoObstaclesX(x1, y1, y2)) 
                            {
                                BombList[bomb2].param1 = time1; 
                            }
                            if (y1 == y2 && Math.Abs(x2 - x1) < range1 && NoObstaclesY(y1, x1, x2))
                            {
                                BombList[bomb2].param1 = time1;
                            }                     
                        }               
                    }
                }                                           
            }
        }
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int result = 20;
                foreach (Entity bomb in BombList)
                {           
                    int range = bomb.param2;                    
                    int time = bomb.param1;
                    if (time < result)
                    {
                        if ((bomb.x == i && Math.Abs(bomb.y - j) < range && NoObstaclesX(bomb.x, bomb.y, j)) ||
                            (bomb.y == j && Math.Abs(bomb.x - i) < range && NoObstaclesY(bomb.y, bomb.x, i)) ||
                            (bomb.x == i && bomb.y == j)) 
                        {
                            result = time; 
                        }
                    }                        
                }                
                gridC[i, j].safe = result;             
            } 
        }   
    }

    static void OrganizeBoxCountLevels(int explosionRange)
    {
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                gridC[i,j].boxCount = BoxCountC(i, j, explosionRange);
            }
        }
    }

    static bool ImitatePlantingBombOnCurrentPlace(int X, int Y, ref Coord shelter, ref Coord shelter1, ref Coord shelter2, ref Coord shelter3, ref int newBombTicksAdjusted, ref int newBombBoxCount)
    {
        bool canPutBomb = gridC[X, Y].type != 'b' && MyPlayer.bombsLeft > 0;
        newBombBoxCount = gridC[X, Y].boxCount;
        if (canPutBomb)
        {
            List<Entity> TempBombList = new List<Entity>(BombList.Count); 
            BombList.ForEach((bomb) => { TempBombList.Add(new Entity(bomb)); });           
            
            foreach (Entity gamer in GamerList)
            {
                if (gamer.param1 > 0)
                {
                    BombList.Add(new Entity(gamer.x, gamer.y, gamer.param2, 7));
                    //gridC[gamer.x, gamer.y].type = 'b';
                }
            }
            OrganizeCellGrid(X, Y, MyPlayer.explosionRange);               
            shelter = FindShelterC(X, Y, 5, 10); 
            shelter1 = FindShelterC(X, Y, 1, 4); 
            shelter2 = FindShelterC(X, Y, 2, 5); 
            shelter3 = FindShelterC(X, Y, 3, 6);  
            newBombTicksAdjusted = 8;
            newBombBoxCount = BoxCountIgnoreCurrent(X, Y, MyPlayer.explosionRange);
            foreach (Entity bomb in BombList)
            {
                if (bomb.x == X && bomb.y == Y)
                {
                    newBombTicksAdjusted = bomb.param1;
                }
            }

            canPutBomb = !MyPlayer.coord.Equals(shelter);                
            BombList.Clear();
            BombList = new List<Entity>(TempBombList.Count); 
            TempBombList.ForEach((bomb) => { BombList.Add(new Entity(bomb)); });   
            OrganizeCellGrid(X, Y, MyPlayer.explosionRange);
        }
        return canPutBomb;
    }

    static void MarkBombsOnGrid()
    {
        for (int j = 0; j < height; j++) 
        {
            for (int i = 0; i < width; i++) 
            {
                if (gridC[i, j].type == 'b')
                {
                    gridC[i, j].type = '.';
                }
            }
        }
        foreach (Entity bomb in BombList)
        {
            gridC[bomb.x, bomb.y].type = 'b';
        }
    }

    static void OrganizeCellGrid(int X, int Y, int explosionRange)
    {
        MarkBombsOnGrid();
        OrganizeAccessLevels(X, Y);
        OrganizeSafetyLevels();
        OrganizeBoxCountLevels(explosionRange);        
    }

    static void ReadGridAndEntityData(int id)
    {
        turn++;
        for (int j = 0; j < height; j++)
        {
            string row = Console.ReadLine();
            for (int i = 0; i < width; i++) 
            {
                gridC[i, j].type = row[i];      
            }           
        }                     
        int entities = int.Parse(Console.ReadLine());
        ItemList.Clear();
        BombList.Clear(); 
        GamerList.Clear();
        for (int i = 0; i < entities; i++)
        {              
            Entity currentEntity = new Entity(Console.ReadLine());
            
            if (currentEntity.entityType == 0)
            { 
                GamerList.Add(currentEntity);
            }
            if (currentEntity.entityType == 2)
            { 
                ItemList.Add(currentEntity);
            }
            if (currentEntity.entityType == 1)
            { 
                //gridC[currentEntity.x, currentEntity.y].type = 'b';
                BombList.Add(currentEntity);
            }
            if (currentEntity.entityType == 0 && currentEntity.owner == id)  
            {                                
                MyPlayer.coord.X = currentEntity.x;
                MyPlayer.coord.Y = currentEntity.y; 
                MyPlayer.bombsLeft = currentEntity.param1;
                MyPlayer.explosionRange = currentEntity.param2;
                MyPlayer.lastBomb++; 
                if (turn <= 1)
                {
                    MyPlayer.destination = TheRichestBoxSpotC(MyPlayer.coord, 5);
                }               
            }
        }
    }

    static int TotalBoxCount()
    {
        int result = 0;   
        for (int j = 0; j < height; j++) 
        {
            for (int i = 0; i < width; i++) 
            {
                if (gridC[i, j].type != '.' && gridC[i, j].type != 'b' && gridC[i, j].type != 'X' && !UnderFire(i, j)) 
                {
                    result++;  
                }
            }
        }    
        return result;
    }  

    static int BoxCountIgnoreCurrent(int X, int Y, int explosionRange)
    {
        int result = 0;      
        
            bool lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)
            {                                  
                if (X - i >= 0)
                {
                    if (gridC[X - i, Y].type == 'X' || gridC[X - i, Y].type == 'b')
                    {
                        lineBlocked = true;
                    }
                    foreach (Entity item in ItemList)
                    {
                        if (item.x == X - i && item.y == Y && !UnderFire(X - i, Y))
                        {
                            lineBlocked = true; 
                        }
                    }
                    if (!lineBlocked)
                    {
                        if (gridC[X - i, Y].type != '.')
                        {
                           
                                result++; 
                            
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
            lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)
            {                   
                if (Y - i >= 0)
                {
                    if (gridC[X, Y - i].type =='X' || gridC[X, Y - i].type == 'b')
                    {
                        lineBlocked = true;
                    } 
                    foreach (Entity item in ItemList) 
                    {
                        if (item.x == X && item.y == Y - i && !UnderFire(X, Y - i))
                        {
                            lineBlocked = true; 
                        } 
                    }
                    if (!lineBlocked)
                    { 
                        if (gridC[X, Y - i].type != '.')
                        {
                            
                                result++;
                            
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
            lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)  
            {                  
                if (X + i < width)
                {
                    if (gridC[X + i, Y].type =='X' || gridC[X + i, Y].type == 'b')
                    {
                        lineBlocked = true;
                    }
                    foreach (Entity item in ItemList)
                    {
                        if (item.x == X + i && item.y == Y && !UnderFire(X + i, Y))
                        {
                            lineBlocked = true; 
                        }
                    }
                    if (!lineBlocked)
                    {
                        if (gridC[X + i, Y].type != '.')
                        {
                            
                                result++;
                            
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
            lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)  
            {                  
                if (Y + i < height)
                {
                    if (gridC[X, Y + i].type =='X' || gridC[X, Y + i].type == 'b') lineBlocked = true;
                    foreach (Entity item in ItemList)
                    {
                        if (item.x == X && item.y == Y + i && !UnderFire(X, Y + i))
                        {
                            lineBlocked = true;
                        }
                    } 
                    if (!lineBlocked)
                    {
                        if (gridC[X, Y + i].type != '.') 
                        {
                            
                                result++;
                            
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
                
        return result; 
    }

    static int BoxCountC(int X, int Y, int explosionRange)
    {
        int result = 0;      
        if (gridC[X, Y].type == '.')
        {
            bool lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)
            {                                  
                if (X - i >= 0)
                {
                    if (gridC[X - i, Y].type == 'X' || gridC[X - i, Y].type == 'b')
                    {
                        lineBlocked = true;
                    }
                    foreach (Entity item in ItemList)
                    {
                        if (item.x == X - i && item.y == Y && !UnderFire(X - i, Y))
                        {
                            lineBlocked = true; 
                        }
                    }
                    if (!lineBlocked)
                    {
                        if (gridC[X - i, Y].type != '.')
                        {
                            if (!UnderFire(X - i, Y))
                            {
                                result++; 
                            }
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
            lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)
            {                   
                if (Y - i >= 0)
                {
                    if (gridC[X, Y - i].type =='X' || gridC[X, Y - i].type == 'b')
                    {
                        lineBlocked = true;
                    } 
                    foreach (Entity item in ItemList) 
                    {
                        if (item.x == X && item.y == Y - i && !UnderFire(X, Y - i))
                        {
                            lineBlocked = true; 
                        } 
                    }
                    if (!lineBlocked)
                    { 
                        if (gridC[X, Y - i].type != '.')
                        {
                            if (!UnderFire(X, Y - i))
                            {
                                result++;
                            }
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
            lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)  
            {                  
                if (X + i < width)
                {
                    if (gridC[X + i, Y].type =='X' || gridC[X + i, Y].type == 'b')
                    {
                        lineBlocked = true;
                    }
                    foreach (Entity item in ItemList)
                    {
                        if (item.x == X + i && item.y == Y && !UnderFire(X + i, Y))
                        {
                            lineBlocked = true; 
                        }
                    }
                    if (!lineBlocked)
                    {
                        if (gridC[X + i, Y].type != '.')
                        {
                            if (!UnderFire(X + i, Y))
                            {
                                result++;
                            } 
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
            lineBlocked = false;
            for (int i = 1; i < explosionRange; i++)  
            {                  
                if (Y + i < height)
                {
                    if (gridC[X, Y + i].type =='X' || gridC[X, Y + i].type == 'b') lineBlocked = true;
                    foreach (Entity item in ItemList)
                    {
                        if (item.x == X && item.y == Y + i && !UnderFire(X, Y + i))
                        {
                            lineBlocked = true;
                        }
                    } 
                    if (!lineBlocked)
                    {
                        if (gridC[X, Y + i].type != '.') 
                        {
                            if (!UnderFire(X, Y + i))
                            {
                                result++;
                            }
                            lineBlocked = true;
                        }
                    }
                    if (lineBlocked)
                    {
                        break;
                    }
                }
            }
        }        
        return result;
    }   

    static bool NoItemInBetween(int x1, int y1, int x2, int y2, int step)
    {
        bool result = true;
        int maxX = Math.Max(x1, x2);
        int maxY = Math.Max(y1, y2);
        if (step <= 2)
        {
            if (x1 == x2)
            {               
                if ((y2 > y1 && IsThereAnyItem(x2, y2 - 1)) || (y2 < y1 && IsThereAnyItem(x2, y2 + 1)) || IsThereAnyItem(x2, y2))
                {
                    result = false;
                } 
            }
            if (y1 == y2)
            {               
                if ((x2 > x1 && IsThereAnyItem(x2 - 1, y2)) || (x2 < x1 && IsThereAnyItem(x2 + 1, y2)) || IsThereAnyItem(x2, y2))
                {
                    result = false;
                } 
            }
        }
        return result;
    } 

    static Coord FindShelterC(int X, int Y, int radius, int minimumSafety)
    {
        int bestSafety = 0;
        Coord result = new Coord(X, Y);       
        for (int i = X - radius; i <= X + radius; i++)
        {
            for (int j = Y - radius; j <= Y + radius; j++)
            {
                if (i >= 0 && j >= 0 && i < width && j < height)
                {

                    if ((gridC[i, j].safe >= minimumSafety || gridC[i, j].safe == 1) && gridC[i, j].access <= radius && IsPathSafe(i, j) && NoItemInBetween(X, Y, i, j, radius)) 
                    {
                        if (gridC[i, j].safe > bestSafety)
                        { 
                            result.X = i; 
                            result.Y = j; 
                            bestSafety = gridC[i, j].safe;
                        } 
                        if (gridC[i,j].safe == bestSafety)
                        {
                            if (IsThereAnyItem(i, j) || gridC[i, j].boxCount > gridC[result.X, result.Y].boxCount ||
                               (gridC[i, j].boxCount == gridC[result.X, result.Y].boxCount && 
                                Distance(result, new Coord(X, Y)) > Distance(new Coord(i, j), new Coord(X, Y))))
                            {                  
                                result.X=i; 
                                result.Y=j;
                            }    
                        }
                    }                               
                }
            }
        }
        return result;
    }

    static bool IsThereAnyItem(int X, int Y)
    {
        bool result = false;
        foreach (Entity item in ItemList)
        {
            if (item.x == X && item.y == Y)
            {
                result = true;
            }
        }           
        return result;
    }

    static Coord ClosestItem(int X, int Y)
    {
        Coord result = new Coord(20, 20);
        foreach (Entity item in ItemList)
        {
            if (gridC[item.x, item.y].access < 5)
            {
                if (Distance(item.coord(), new Coord(X, Y)) < Distance(result, new Coord(X, Y)))
                {
                    result = item.coord();
                }
                else if (Distance(item.coord(), new Coord(X, Y)) == Distance(result, new Coord(X, Y)) &&
                         gridC[item.x, item.y].boxCount > gridC[result.X, result.Y].boxCount)
                {
                    result = item.coord();
                }
                
            }
        }           
        return result;
    }

    static int Safe(int X, int Y)
    {
        int result = 20;
        if (X >= 0 && Y >= 0 && X < width && Y < height) 
        {
            result = gridC[X,Y].safe;
        }
        return result;
    }

    static int Distance(Coord coord1, Coord coord2)
    {
        return Math.Abs(coord1.X - coord2.X) + Math.Abs(coord1.Y - coord2.Y);
    }

    static void SetDestination(NewPlayer player, Coord destination, string addition)
    {
        player.destination = destination;
        Console.WriteLine($"MOVE {destination.X} {destination.Y}" + addition);       
    }

    static void PutBombAndRunTo(NewPlayer player, Coord destination, string addition)
    {
        player.destination = destination;
        Console.WriteLine($"BOMB {destination.X} {destination.Y}" + addition); 
        player.lastBomb = 0;      
    }

    static Coord TheRichestBoxSpotC(Coord point, int maxStep)
    {
        int maximumBoxCount = 0; 
          
        Coord result = new Coord(0, 0);       
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)       
            {
                if (Math.Abs(point.X - i) <= maxStep && Math.Abs(point.Y - j) <= maxStep)
                {                  
                    if (gridC[i, j].access <= maxStep)
                    {
                        int currentBoxCount = gridC[i, j].boxCount;
                        if ((i == 0 && j == 0) || (i == 0 && j == height - 1) || (i == width - 1 && j == 0) || (i == width - 1 && j == height - 1))
                        {
                            currentBoxCount =0;
                        }
                        if (currentBoxCount > maximumBoxCount)
                        {
                            maximumBoxCount = currentBoxCount;
                            result.X = i;
                            result.Y = j;
                        }    
                        else if (currentBoxCount == maximumBoxCount) 
                        {
                            if (Distance(point, new Coord(i, j)) < Distance(point, result) || IsThereAnyItem(i, j))
                            {
                                result.X = i;
                                result.Y = j;
                            }
                        }               
                    }
                }
            }           
        }      
        return result;
    }

    static bool ExistsAccessAround(int X, int Y, int accessLevel, Cell[,] pathGrid)
    {
        bool result = false;
        if (X - 1 >= 0)
        {
            if (pathGrid[X - 1, Y].access == accessLevel)
            {
                result = true;
            }
        }
        if (X + 1 < width)
        {
            if (pathGrid[X + 1, Y].access == accessLevel)
            {
                result = true;
            }
        }
        if (Y - 1 >= 0)
        {
            if (pathGrid[X, Y - 1].access == accessLevel)
            {
                result = true;
            }
        }
        if (Y + 1 < height)
        {
            if (pathGrid[X, Y + 1].access == accessLevel)
            {
                result = true;
            }
        }
        return result;
    }

    static bool ExistsSafetyAround(int X, int Y, int safetyLevel)
    {
        bool result = false;
        if (X - 1 >= 0)
        {
            if (gridC[X - 1, Y].access < 10 && gridC[X - 1, Y].safe >= safetyLevel)
            {
                result = true;
            }
        }
        if (X + 1 < width)
        {
            if (gridC[X + 1, Y].access < 10 && gridC[X + 1, Y].safe >= safetyLevel)
            {
                result = true;
            }
        }
        if (Y - 1 >= 0)
        {
            if (gridC[X, Y - 1].access < 10 && gridC[X, Y - 1].safe >= safetyLevel)
            {
                result = true;
            }
        }
        if (Y + 1 < height)
        {
            if (gridC[X, Y + 1].access < 10 && gridC[X, Y + 1].safe >= safetyLevel)
            {
                result = true;
            }
        }
        return result;
    }

    static bool IsPathSafe(int targetX, int targetY)
    {  
        bool result = true;
        Cell[,] pathGrid = new Cell[width, height]; 
        for (int j = 0; j < height; j++)
        { 
            for (int i = 0; i < width; i++) 
            {
                pathGrid[i, j].access = 100;  
            }   
        }   
                 
        pathGrid[targetX, targetY].access = gridC[targetX, targetY].access;
        for (int step = gridC[targetX, targetY].access - 1; step >= 0; step--)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++) 
                {
                    if (gridC[i, j].access == step && pathGrid[i, j].access == 100 && ExistsAccessAround(i, j, step + 1, pathGrid))
                    {
                        pathGrid[i, j].access = step;
                        if (step <= 3 && Safe(i, j) <= step + 1)
                        {
                            result = false;
                        }
                    }                                           
                }
            }
        } 
        return result;  
    }

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        width = int.Parse(inputs[0]);
        height = int.Parse(inputs[1]);
        int myId = int.Parse(inputs[2]);
        gridC = new Cell[width, height];

        while (true)
        {
            ReadGridAndEntityData(myId);
            int X = MyPlayer.coord.X;
            int Y = MyPlayer.coord.Y;          
            OrganizeCellGrid(X, Y, MyPlayer.explosionRange); 

            Coord shelter = new Coord(0, 0);
            Coord shelter1 = new Coord(0, 0);
            Coord shelter2 = new Coord(0, 0);
            Coord shelter3 = new Coord(0, 0);
            int newBombTicks = 8;
            int newBombBoxCount = 0;
            bool CanPutBombNow = ImitatePlantingBombOnCurrentPlace(X, Y, ref shelter, ref shelter1, ref shelter2, ref shelter3, ref newBombTicks, ref newBombBoxCount);

            if (Safe(X, Y) <= 2)
            {                             
                SetDestination(MyPlayer, FindShelterC(X, Y, 1, 3), " Not SAFE RUN (1)");
            } 
            else if (Safe(X + 1, Y) <= 2 || Safe(X - 1, Y) <= 2 || Safe(X, Y + 1) <= 2 || Safe(X, Y - 1) <= 2)
            {
                if (Safe(X, Y) <= 3)
                { 
                    SetDestination(MyPlayer, FindShelterC(X, Y, 2, 4), " Not SAFE RUN (2)");
                }
                else
                {
                    Console.WriteLine($"MOVE {X} {Y} Not SAFE Around! Stay");
                } 
            }
            else if (Safe(X, Y) <= 3)
            {             
                //shelter = FindShelterC(X, Y, 2, 8);
                if (bombIsAdequateHere(X, Y) && CanPutBombNow && !shelter2.Equals(MyPlayer.coord))
                {
                    PutBombAndRunTo(MyPlayer, shelter2, " BOMB! and run (1) super fast");   
                }
                else
                {
                    SetDestination(MyPlayer, FindShelterC(X, Y, 2, 4), " Not SAFE RUN (3)");
                }
            } 
            else if (Safe(X, Y) <= 4)
            {             
                //shelter = FindShelterC(X, Y, 3, 10);
                if (bombIsAdequateHere(X, Y) && CanPutBombNow && !shelter3.Equals(MyPlayer.coord))
                {
                    PutBombAndRunTo(MyPlayer, shelter3, " BOMB! and run (2) very fast");   
                }
                else
                {
                    SetDestination(MyPlayer, FindShelterC(X, Y, 3, 5), " Not SAFE RUN (4)");
                }
            }   
            else 
            {
                Coord closestItem = ClosestItem(X, Y);
                if (BoxCountC(X, Y, MyPlayer.explosionRange) > 2 && CanPutBombNow && !shelter3.Equals(MyPlayer.coord))
                {                                                      
                    PutBombAndRunTo(MyPlayer, shelter3, " BOMB (3) RUN AWAY, guys");                                                        
                }  
                else if (MyPlayer.bombsLeft < 6 && Distance(MyPlayer.coord, closestItem) <= 1 && gridC[closestItem.X, closestItem.Y].safe > 4 && newBombTicks >=4 && ExistsSafetyAround(closestItem.X, closestItem.Y, 6) && !shelter1.Equals(MyPlayer.coord))
                {
                    if (bombIsAdequateHere(X, Y) && CanPutBombNow && closestItem.X != X && closestItem.Y != Y)
                    {
                        PutBombAndRunTo(MyPlayer, closestItem, " BOMB! Wow Item(1), guys! Nice!");   
                    }
                    else 
                    {
                        SetDestination(MyPlayer, closestItem, " Item (1) here! Wow Nice!");
                    }
                }              
                else if (MyPlayer.bombsLeft < 6 && Distance(MyPlayer.coord, closestItem) <= 2 && gridC[closestItem.X, closestItem.Y].safe > 5 && newBombTicks >=5 && ExistsSafetyAround(closestItem.X, closestItem.Y, 7) && !shelter2.Equals(MyPlayer.coord))
                {
                   if (bombIsAdequateHere(X, Y) && CanPutBombNow && closestItem.X != X && closestItem.Y != Y)
                    {
                        PutBombAndRunTo(MyPlayer, closestItem, " BOMB! Wow Item(2), guys! Nice!");   
                    }
                    else
                    {
                        SetDestination(MyPlayer, closestItem, " Item (2) so close! Nice!");
                    }
                }
                else if (BoxCountC(X, Y, MyPlayer.explosionRange) > 1 && CanPutBombNow)
                {                                                       
                    PutBombAndRunTo(MyPlayer, shelter, " BOMB (2) RUN AWAY, guys");                                                               
                }
                else if (MyPlayer.bombsLeft < 6 && Distance(MyPlayer.coord, closestItem) <= 3  && gridC[closestItem.X, closestItem.Y].safe > 6)
                {
                    if (CanPutBombNow && gridC[X, Y].boxCount > 0 && gridC[closestItem.X, closestItem.Y].safe > 6 && closestItem.X != X && closestItem.Y != Y)
                    {
                        PutBombAndRunTo(MyPlayer, closestItem, " BOMB! Wow Item, guys! Nice!");    
                    }
                    else SetDestination(MyPlayer, closestItem, " item (3) move");  
                }                
                else if ((BoxCountC(X, Y, MyPlayer.explosionRange) > 0 || (newBombBoxCount > 0 && MyPlayer.bombsLeft > 1 && MyPlayer.lastBomb > 1)) && CanPutBombNow)
                {
                    Coord richestPointAround = TheRichestBoxSpotC(MyPlayer.coord, 1);
                    if (richestPointAround.Equals(MyPlayer.coord) || MyPlayer.destination.Equals(MyPlayer.coord) || bombIsAdequateHere(X, Y)) 
                    {                     
                        PutBombAndRunTo(MyPlayer, shelter, " BOMB (1) RUN AWAY, guys");                                   
                    }
                    else if (gridC[richestPointAround.X, richestPointAround.Y].boxCount > 1 && gridC[richestPointAround.X, richestPointAround.Y].safe > 3)
                    {
                        SetDestination(MyPlayer, richestPointAround, " more boxes are close");
                    }
                    else if (MyPlayer.lastBomb > 3) 
                    {                     
                        PutBombAndRunTo(MyPlayer, shelter, " BOMB (1) RUN AWAY, guys");                                   
                    } 
                    else SetDestination(MyPlayer, MyPlayer.destination, " just MOVE ");           
                }                   
                else if (MyPlayer.bombsLeft < 6 && Distance(MyPlayer.coord, closestItem) <= 4  && gridC[closestItem.X, closestItem.Y].safe > 7)
                {
                    SetDestination(MyPlayer, closestItem, " item (4) move");  
                }   
                else if (TotalBoxCount() == 0)
                {                                                       
                    SetDestination(MyPlayer, new Coord(10, 10), " FINISH BURN IT ALL!");                                                            
                }             
                else if (!MyPlayer.destination.Equals(MyPlayer.coord))
                {
                    if (MyPlayer.coord.Equals(new Coord(0, 0)))
                    {
                        SetDestination(MyPlayer, MyPlayer.destination, " just MOVE ");
                    }
                    else  SetDestination(MyPlayer, TheRichestBoxSpotC(MyPlayer.coord, 15), " MOVE far, get rich");
                }             
                else
                {
                    SetDestination(MyPlayer, TheRichestBoxSpotC(MyPlayer.coord, 9), " MOVE close, get rich");  
                } 
            }
        }
    }
}