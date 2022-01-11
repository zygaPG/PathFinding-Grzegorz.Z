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
    Text x_grid_field;
    [SerializeField]
    Text y_grid_field;

    [SerializeField]
    Transform arena;

    [SerializeField]
    GameObject quad_pref;

    int grid_x_size;
    int grid_y_size;

    [SerializeField]
    int2 start_Position = new int2(-1, -1);

    [SerializeField]
    int2 target_Position = new int2(-1, -1);

    int[] wallPositions_Encrypted;             // wall.x + wall.y * grid.x 

    int2[] currentPath;

    public void Generate_Grid()
    {
        foreach (Transform child in arena)
        {
            Destroy(child.gameObject);
        }

        start_Position.x = -1;
        target_Position.x = -1;

        grid_x_size = int.Parse(x_grid_field.text);
        grid_y_size = int.Parse(y_grid_field.text);


        float xSize;
        float ySize;

        float size;

        xSize = Screen.width / grid_x_size;
        ySize = Screen.height / grid_y_size;

        if (xSize < ySize)
        {
            arena.GetComponent<RectTransform>().sizeDelta = new Vector2(xSize * grid_x_size, xSize * grid_y_size);
            size = xSize;
        }
        else
        {
            arena.GetComponent<RectTransform>().sizeDelta = new Vector2(ySize * grid_x_size, ySize * grid_y_size);
            size = ySize;
        }



        for (int x = 0; x < grid_x_size; x++)
        {
            for (int y = 0; y < grid_y_size; y++)
            {
                RectTransform tr = Instantiate(quad_pref, arena).GetComponent<RectTransform>();
                tr.sizeDelta = new Vector2(size, size);
                tr.anchoredPosition = new Vector3(  (size / 2) + (x * size), 
                                                    -((size / 2) + (y * size)), 
                                                 0);
                
                tr.transform.name = (x + 1) + "v" + (y + 1);
                tr.GetComponent<Button>().onClick.AddListener(() => ButtonClick(tr.transform.name));

            }
        }



        List<int> new_Walls = WallGenerator(new int2(grid_x_size, grid_y_size));
        foreach (int wallPos in new_Walls)
        {
            SetButtonCollor(wallPos, wallColor);
        }
        wallPositions_Encrypted = new_Walls.ToArray();

    }


    public void ButtonClick(string buttonName)        //
    {
        if (start_Position.x == -1)
        {
            start_Position = Decrypt_Button(buttonName);
            SetButtonCollor(start_Position, startColor);
        }
        else
        {
            if (target_Position.x != -1 && currentPath != null)
            {
                foreach (int2 quadPosition in currentPath)
                {
                    SetButtonCollor(quadPosition, new Color32(255, 255, 255, 255));
                }
                SetButtonCollor(target_Position, new Color32(255, 255, 255, 255));
            }

            target_Position = Decrypt_Button(buttonName);
            SetButtonCollor(target_Position, targetColor);

            StartPathFinding();


        }
    }
    
    public int2 Decrypt_Button(string _name)    //decrypting button name string "3v15" to int2 {3,15}
    {
        string[] splitArray =  _name.Split('v');

        int2 retPosition = new int2(
            int.Parse(splitArray[0]) - 1, 
            int.Parse(splitArray[1]) - 1);
       
        return retPosition;
    }
    

    void SetButtonCollor(int2 position, Color32 colo)
    {
        //int quad_num = ((position.x) * grid_y_size) + position.y + 1;
        int quad_num = position.x + position.y * grid_x_size;
        arena.transform.GetChild(quad_num ).GetComponent<Image>().color = colo;
    }

    void SetButtonCollor(int position, Color32 colo)
    {
        arena.transform.GetChild(position).GetComponent<Image>().color = colo;

    }



    //-------------------------------------------------------------------------------------

    void StartPathFinding()
    {
        PathFindJob pathFindJob = new PathFindJob
        {
            grid_x = grid_x_size,
            grid_y = grid_y_size,
            startPosition = start_Position,
            targetPosition = target_Position,
            wall = new NativeArray<int>(wallPositions_Encrypted.Length, Allocator.TempJob)

        };
        pathFindJob.wall.CopyFrom(wallPositions_Encrypted);

        pathFindJob.Execute();
        

        foreach (int2 quadPosition in pathFindJob.path)
        {
            SetButtonCollor(quadPosition, pathColor);

        }

        currentPath = pathFindJob.path.ToArray();

        pathFindJob.wall.Dispose();
        pathFindJob.path.Dispose();

    }



    [BurstCompatible]
    struct PathFindJob : IJob
    {
        public int grid_x;
        public int grid_y;

        public int2 startPosition;
        public int2 targetPosition;

        public NativeList<int2> path;
        public NativeArray<int> wall;

        public void Execute()
        {
            NativeArray<Quad> quadsArray = new NativeArray<Quad>(grid_x * grid_y, Allocator.Temp);

            for (int i = 0; i < grid_x; i++)
            {
                for (int o = 0; o < grid_y; o++)
                {
                    Quad quad = new Quad();
                    quad.x = i;
                    quad.y = o;
                    quad.id = Calculate_id(i, o);

                    quad.gValue = int.MaxValue;
                    quad.hValue = Calculate_gValue(i, o);
                    quad.Set_fValue();

                    quad.open = true;

                    quad.owner_id = -1;

                    

                    quadsArray[quad.id] = quad;
                }
            }

            foreach(int wal_Id in wall)                   // set quad.open = true  on wall positions
            {
                Quad currentQuad =  quadsArray[wal_Id];
                currentQuad.open = false;
                quadsArray[wal_Id] = currentQuad;
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
            startQuad.gValue = 0;
            startQuad.Set_fValue();
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

                    int newG_Value = newQuad.gValue + (i > 3 ? 14 : 10); // slant + 14, normal +10
                    if (newG_Value < newQuad.gValue)
                    {
                        newQuad.owner_id = currentQuadId;
                        newQuad.gValue = newG_Value;
                        newQuad.Set_fValue();
                        quadsArray[newId] = newQuad;

                        if (!openList.Contains(newQuad.id))
                        {
                            openList.Add(newQuad.id);
                        }
                    }

                }

            }






            Quad endQuad = quadsArray[targetQuadId];

            if (endQuad.owner_id == -1)
            {
                // dont find path
                Debug.Log("cat find path");
            }
            else
            {
                // found
                path = ShowPath(quadsArray, endQuad);
            }
            path.RemoveAt(0);
            path.RemoveAt(path.Length - 1);

            //path.Dispose();
            quadsArray.Dispose();
            offsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();

        }



        bool ValidPosition(int2 pos)
        {
            return
                pos.x >= 0 && pos.x <= grid_x &&
                pos.y >= 0 && pos.y <= grid_y;
        }

        int Calculate_id(int x, int y)
        {
            return x + y * grid_x;
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
                path.Add(new int2(endQuad.x, endQuad.y));

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
                if (testQuad.fValue < lowestQuad.fValue)
                {
                    lowestQuad = testQuad;
                }
            }

            return lowestQuad.id;
        }

        int Calculate_gValue(int xPos, int yPos)
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
        List<int> wallEncrypted = new List<int>(); 

        for(int i=0; i<= firstGen; i++)
        {
            int2 newWall = new int2(
                UnityEngine.Random.Range(0, grid_size.x), 
                UnityEngine.Random.Range(0, grid_size.y)    );

            int new_Wall_Encrypted = newWall.x + newWall.y * grid_size.x;

            if (!wallEncrypted.Exists(x => x == new_Wall_Encrypted))
            {
                wallEncrypted.Add(new_Wall_Encrypted);              //  x + y * grid.x
                wall.Add(newWall);
            }
            else
            {
                //if(i>0)
                //i--;
            }
        }

        if(wall.Count == 0)
        {
            int2 newWall = new int2(
                UnityEngine.Random.Range(0, grid_size.x),
                UnityEngine.Random.Range(0, grid_size.y));

            int new_Wall_Encrypted = newWall.x + newWall.y * grid_size.x;

            wallEncrypted.Add(new_Wall_Encrypted);              //  x + y * grid.x
            wall.Add(newWall);
        }

        
        int lastWallLengt = wall.Count;

        for (int i=0; i< wall.Count; i++)
        {
            if(wallEncrypted.Count < wallMaxCout)
            {
                int2 currentWall = wall[i];

                for(int o=0; o< firstGen; o++)
                {

                    for(int p=0; p < firstGen; p++){

                        int2 new_Way = new int2(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
                        currentWall += new_Way;

                        if (currentWall.x < 0 && currentWall.x > grid_size.x && currentWall.y < 0 && currentWall.y > grid_size.y)
                        {
                            if (p > 0)
                                p--;
                            currentWall -= new_Way;
                            continue;
                        }

                        int currentWall_Encrypted = currentWall.x + currentWall.y * grid_size.x;

                        if (wallEncrypted.Exists(x => x == currentWall_Encrypted))
                        {
                            if (p > 0)
                                p--;
                            continue;
                        }

                        wallEncrypted.Add(currentWall_Encrypted);


                    }



                }


            }


        }
        
   

        return wallEncrypted;
    }




    public struct Quad
    {
        public int x;
        public int y;

        public int id;
        public int owner_id;

        public int gValue; // this -> owner
        public int fValue; // g + h
        public int hValue; // this -> target

        public bool open;

        public void Set_fValue()
        {
            fValue = gValue + hValue;
        }

    }

    

    



}
