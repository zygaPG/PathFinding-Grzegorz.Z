using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;

using Unity.Mathematics;
using Unity.Jobs;


public class Grid_Generator : MonoBehaviour
{
    [SerializeField]
    Color32 startColor;
    [SerializeField]
    Color32 targetColor;
    [SerializeField]
    Color32 pathColor;
    [SerializeField]
    Color32 wallColor;

    [SerializeField]
    Text XAreaTextField;
    [SerializeField]
    Text YAreaTextField;

    [SerializeField]
    Transform arenaParent;
    [SerializeField]
    GameObject arenaPrefab;

    GameObject curretArena;

    [SerializeField]
    GameObject buttonPrefab;

    int arenaWidth;
    int arenaHeight;

    [SerializeField]
    int2 startPosition = new int2(-1, -1);

    [SerializeField]
    int2 targetPosition = new int2(-1, -1);

    int[] wallIdentity;             // wall.x + wall.y * grid.x 

    int2[] currentPath;

    public void Generate_Grid()
    {
        if(curretArena)
        Destroy(curretArena.gameObject);

        curretArena = Instantiate(arenaPrefab, arenaParent);

        if(curretArena == null)
        {
            return;
        }

        startPosition.x = -1;
        targetPosition.x = -1;

        arenaWidth   = int.Parse(XAreaTextField.text);
        arenaHeight = int.Parse(YAreaTextField.text);


        float xSize;
        float ySize;

        float size;

        xSize = Screen.width / arenaWidth;
        ySize = Screen.height / arenaHeight;

        if (xSize < ySize)
        {
            curretArena.GetComponent<RectTransform>().sizeDelta = new Vector2(xSize * arenaWidth,     xSize * arenaHeight);
            size = xSize;
        }
        else
        {
            curretArena.GetComponent<RectTransform>().sizeDelta = new Vector2(ySize * arenaWidth,     ySize * arenaHeight);
            size = ySize;
        }



        for (int x = 0; x < arenaWidth; x++)
        {
            for (int y = 0; y < arenaHeight; y++)
            {
                RectTransform tr = Instantiate(buttonPrefab, curretArena.transform).GetComponent<RectTransform>();

                tr.sizeDelta = new Vector2(size, size);
                tr.anchoredPosition = new Vector3(  (size / 2) + (x * size), 
                                                    -((size / 2) + (y * size)), 
                                                                                 0);

                tr.transform.name = (x + 1) + "v" + (y + 1);
                tr.GetComponent<Button>().onClick.AddListener(() => ButtonClick(tr.transform.name));
            }
        }


        
        List<int> new_Walls = WallGenerator(new int2(arenaWidth, arenaHeight));
        foreach (int wallidentity in new_Walls)
        {
            Debug.Log(wallidentity);
            SetButtonCollor(wallidentity, wallColor);
        }
        wallIdentity = null;
        wallIdentity = new_Walls.ToArray();
        


    }








    public void ButtonClick(string buttonName)        
    {
        if (startPosition.x == -1)
        {
            startPosition = StrigToInt2(buttonName);
            SetButtonCollor(startPosition, startColor);
            curretArena.transform.GetChild(GenerateId(startPosition)).GetChild(0).GetComponent<Text>().text = "x" + startPosition.x + " y" + startPosition.y;
        }
        else
        {
            if (targetPosition.x != -1 && currentPath != null)
            {
                foreach (int2 quadPosition in currentPath)
                {
                    SetButtonCollor(quadPosition, new Color32(255, 255, 255, 255));
                    curretArena.transform.GetChild( GenerateId(quadPosition)    ).GetChild(0).GetComponent<Text>().text = "";
                }
                SetButtonCollor(targetPosition, new Color32(255, 255, 255, 255));
                curretArena.transform.GetChild( GenerateId(targetPosition)  ).GetChild(0).GetComponent<Text>().text = "";
            }

            targetPosition = StrigToInt2(buttonName);
            SetButtonCollor(targetPosition, targetColor);
            curretArena.transform.GetChild(GenerateId(targetPosition)).GetChild(0).GetComponent<Text>().text = "x" + targetPosition.x + " y" + targetPosition.y;

            StartPathFinding();


        }
    }
    

    //---------------------------------------------------------------------------------------------------------------------------------


    public int2 StrigToInt2(string _name)    //split string "3v15" to int2 {3,15}
    {
        string[] splitArray =  _name.Split('v');

        int2 retPosition = new int2(
            int.Parse(splitArray[0]) - 1, 
            int.Parse(splitArray[1]) - 1);
       
        return retPosition;
    }

    void SetButtonCollor(int2 buttonPosition, Color32 color)
    {
        curretArena.transform.GetChild( GenerateId(buttonPosition)    ).GetComponent<Image>().color = color;
    }

    void SetButtonCollor(int positionId, Color32 color)
    {
        curretArena.transform.GetChild( positionId  ).GetComponent<Image>().color = color;
    }


    void ShowPathElement(int2 elementPosition, int fCost)
    {
        SetButtonCollor(elementPosition, pathColor);
        curretArena.transform.GetChild( GenerateId(elementPosition)     ).transform.GetChild(0).GetComponent<Text>().text = "x" + elementPosition.x + " y" + elementPosition.y + " f" + fCost;
    }

    int GenerateId(int2 position)
    {
        return (position.x * arenaHeight) + position.y;
    }


    //---------------------------------------------------------------------------------------------------------------------------------

    void StartPathFinding()
    {
        PathFindJob pathFindJob = new PathFindJob
        {
            gridSizeX = arenaWidth,
            gridSizeY = arenaHeight,
            startPosition = startPosition,
            targetPosition = targetPosition,
            wall = new NativeArray<int>(wallIdentity.Length, Allocator.TempJob)

        };

        pathFindJob.wall.CopyFrom(wallIdentity);

        pathFindJob.Execute();

        pathFindJob.wall.Dispose();

        int dystansToStart = pathFindJob.path.Length+1;
        foreach (int2 quadPosition in pathFindJob.path)
        {
            dystansToStart--;
            ShowPathElement(quadPosition, dystansToStart);
        }

        currentPath = pathFindJob.path.ToArray();           // for clear path elements when you need another path on the same arena

        
        pathFindJob.path.Dispose();

    }



    [BurstCompatible]
    struct PathFindJob : IJob
    {
        public int gridSizeX;
        public int gridSizeY;

        public int2 startPosition;
        public int2 targetPosition;

        public NativeList<int2> path;
        public NativeArray<int> wall;

        public void Execute()
        {
            //      Quad - sigle element on grid 

            NativeArray<Quad> quadsArray = new NativeArray<Quad>(gridSizeX * gridSizeY, Allocator.Temp);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Quad quad = new Quad();
                    quad.x = x;
                    quad.y = y;
                    quad.id = Calculate_id(x, y);

                    quad.gCost = int.MaxValue;
                    quad.hCost = Calculate_hCost(x, y);
                    quad.CalculatefCost();

                    quad.open = true;
                    quad.owner_id = -1;

                    quadsArray[quad.id] = quad;
                }
            }

            foreach(int wallIndex in wall)                   // set quad.open = true  on wall positions
            {
                Quad currentQuad =  quadsArray[wallIndex];
                currentQuad.open = false;
                quadsArray[wallIndex] = currentQuad;
            }
            

            NativeArray<int2> offsetArray = new NativeArray<int2>(8, Allocator.Temp);   // cant add elements to array here
            offsetArray[0] = new int2(-1, 0); // left
            offsetArray[1] = new int2(1, 0); //right
            offsetArray[2] = new int2(0, 1); //up
            offsetArray[3] = new int2(0, -1); //down
            offsetArray[4] = new int2(-1, -1); //left Down
            offsetArray[5] = new int2(-1, 1); //left up
            offsetArray[6] = new int2(1, -1); //right down
            offsetArray[7] = new int2(1, 1); //rigt up

            int targetQuadId = Calculate_id(targetPosition.x, targetPosition.y);


            Quad startQuad = quadsArray[Calculate_id(startPosition.x, startPosition.y)];
            startQuad.gCost = 0;
            startQuad.CalculatefCost();
            quadsArray[startQuad.id] = startQuad;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startQuad.id);



            while (openList.Length > 0)
            {
                int currentQuadId = GetLowestF(openList, quadsArray);
                Quad currentQuad = quadsArray[currentQuadId];

                if (currentQuadId == targetQuadId)      //finish!!!!
                {
                    break;
                }

                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentQuadId)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentQuadId);

                for (int i = 0; i < offsetArray.Length; i++)
                {
                    int2 curretOffset = offsetArray[i];
                    int2 newPosition = new int2(currentQuad.x + curretOffset.x, currentQuad.y + curretOffset.y);

                    if (!ValidPosition(newPosition))        // new quad is outisde 
                        continue;

                    int newId = Calculate_id(newPosition.x, newPosition.y);

                    if (closedList.Contains(newId))         // new quad is in losed list
                        continue;

                    Quad newQuad = quadsArray[newId];

                    if (!newQuad.open)                      //its a wall
                        continue;

                    int newG_Value = newQuad.gCost + (i > 3 ? 14 : 10); // slant + 14, normal +10
                    if (newG_Value < newQuad.gCost)
                    {
                        newQuad.owner_id = currentQuadId;
                        newQuad.gCost = newG_Value;
                        newQuad.CalculatefCost();
                        quadsArray[newId] = newQuad;

                        if (!openList.Contains(newQuad.id))
                        {
                            openList.Add(newQuad.id);
                        }
                    }

                }

            }


            Quad endQuad = quadsArray[targetQuadId];


            if (endQuad.owner_id == -1)     // dont find path
            {
                Debug.Log("cat find path");
            }
            else                            // found
            {
                path = ShowPath(quadsArray, endQuad);
            }


            path.RemoveAt(0);
            path.RemoveAt(path.Length - 1);

            quadsArray.Dispose();
            offsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();

        }



        bool ValidPosition(int2 pos)
        {
            return
                pos.x >= 0 && pos.x <= gridSizeX &&
                pos.y >= 0 && pos.y <= gridSizeY;
        }

        int Calculate_id(int x, int y)
        {
            return (x * gridSizeY) + y ;
        }

        NativeList<int2> ShowPath(NativeArray<Quad> quadArray, Quad endQuad)
        {
            if (endQuad.owner_id == -1)  // cant find path :/
            {
                return new NativeList<int2>(Allocator.Temp);
            }
            else
            {
                NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
                path.Add(new int2());

                Quad currQuad = endQuad;

                while (currQuad.owner_id != -1)
                {
                    Quad ownerOwner = quadArray[currQuad.owner_id];
                    path.Add(new int2(ownerOwner.x, ownerOwner.y));

                    currQuad = ownerOwner;

                }

                return path;
            }



        }

        int GetLowestF(NativeList<int> _openList, NativeArray<Quad> _quadArray)
        {
            Quad lowestQuad = _quadArray[_openList[0]];

            for (int i = 1; i < _openList.Length; i++)
            {
                Quad testQuad = _quadArray[_openList[i]];
                if (testQuad.fCost < lowestQuad.fCost)
                {
                    lowestQuad = testQuad;
                }
            }
            Debug.Log(lowestQuad.fCost);
            return lowestQuad.id;
        }

        int Calculate_hCost(int xPos, int yPos)
        {
            int dystX = Mathf.Abs(xPos - targetPosition.x);
            int dystY = Mathf.Abs(yPos - targetPosition.y);
            int result = Mathf.Abs(dystX - dystY);
            return 10 * math.min(dystX, dystY) + 14 * result;
        }
    }



    public List<int> WallGenerator(int2 grid_size){

        int wallMaxCout = (grid_size.x * grid_size.y) / 10;

        int firstGen = wallMaxCout / 5;   // 20% of max 

        


        List<int2> wall = new List<int2>();
        List<int> wallid = new List<int>(); 

        for(int i=0; i<= firstGen; i++)
        {
            int2 newWall = new int2(
                UnityEngine.Random.Range(0, grid_size.x-1), 
                UnityEngine.Random.Range(0, grid_size.y-1)    );

            int new_Wall_Encrypted = GenerateId(newWall);

            if (!wallid.Exists(x => x == new_Wall_Encrypted))
            {
                wallid.Add(new_Wall_Encrypted);              //  x * grid.y + y 
                wall.Add(newWall);
            }
            else
            {
                i--;
            }
        }



        if(wall.Count == 0)
        {
            int2 newWall = new int2(
                UnityEngine.Random.Range(0, grid_size.x-1),
                UnityEngine.Random.Range(0, grid_size.y-1));

            int new_Wall_Encrypted = GenerateId(newWall);

            wallid.Add(new_Wall_Encrypted);              //   x * grid.y + y 
            wall.Add(newWall);
        }


        for (int i=0; i< wall.Count; i++)
        {
            if(wallid.Count < wallMaxCout)
            {
                int2 currentWall = wall[i];

                for(int o=0; o< firstGen; o++)
                {
                    int wallAround = 0;

                    for(int p=0; p < firstGen; p++){

                        int2 new_Way;
                        if (UnityEngine.Random.Range(0, 2) > 0)
                        {
                            new_Way = new int2(UnityEngine.Random.Range(-1, 1),     0);
                            if(new_Way.x == 0)
                            {
                                p--;
                                continue;
                            }
                        }
                        else
                        {
                            new_Way = new int2(0,   UnityEngine.Random.Range(-1, 1));
                            if (new_Way.y == 0)
                            {
                                p--;
                                continue;
                            }
                        }



                        int2 newWall = currentWall + new_Way;
                        //currentWall += new_Way;

                        if (newWall.x < 0 && newWall.x >= grid_size.x && newWall.y < 0 && newWall.y >= grid_size.y)
                        {
                            p--;
                            //currentWall -= new_Way;
                            continue;
                        }

                        int currentWallid = GenerateId(newWall);

                        if (currentWallid < 0)
                        {
                            wallAround++;
                            if (wallAround > 8)
                            {
                                break;
                            }
                            p--;
                            //currentWall -= new_Way;
                            continue;
                        }

                        if (wallid.Exists(x => x == currentWallid))
                        {
                            wallAround++;
                            if (wallAround > 8)
                            {
                                break;
                            }

                            p--;
                            //currentWall -= new_Way;
                            continue;
                        }

                        currentWall = newWall;


                        wallid.Add(currentWallid);

                    }
                }
            }
        }
        
   

        return wallid;
    }


    


    public struct Quad
    {
        public int x;
        public int y;

        public int id;
        public int owner_id;

        public int gCost; // this -> owner
        public int hCost; // this -> target
        public int fCost; // g + h
        

        public bool open;

        public void CalculatefCost()
        {
            fCost = gCost + hCost;
        }

    }

    

    



}
