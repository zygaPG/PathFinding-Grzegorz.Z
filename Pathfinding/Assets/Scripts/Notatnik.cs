using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Notatnik : MonoBehaviour
{
   /*
     public void Pathfindin()
    {
        Check_Neighbour(start_position, 0);

        bool finddd = false;
        int tryyy = 0;

        while (finddd == false)
        {
            tryyy++;
            if(tryyy > 1000)
            {
                finddd = true;
                Debug.Log("end_Time");
            }
          

            int min = int.MaxValue;
            Node[] node_array = new_Nodes.ToArray();
            int array_lowest_id = 0;


            for (int i = 0; i < node_array.Length; i++)
            {
                if (min < node_array[i].f_Value)
                {
                    min = node_array[i].f_Value;
                    array_lowest_id = i;
                }

                if(min == node_array[i].f_Value)
                {

                }
            }
            
            Node lowestNode = node_array[array_lowest_id];
            
            if (checked_Nodes.Find(x => x.position == lowestNode.position) == null)
            {
                Debug.Log("lowest position: " + lowestNode.position[0].ToString() + " " + lowestNode.position[1].ToString());
                finddd = Check_Neighbour(lowestNode.position, lowestNode.h_Value);
                checked_Nodes.Add(lowestNode);
            }
            
        }

    }


    bool Check_Neighbour(int[] _pos, int h_owner)
    {
        int[] start_corner = new int[2];
        start_corner[0] = _pos[0] - 1;
        start_corner[1] = _pos[1] - 1;

        for (int i = 0; i<3; i++)//yy
        {
            for(int o = 0; o < 3; o++)//xx
            {
                int[] pos = { start_corner[0] + i, start_corner[1] + o };

                if(pos[0] >= 0 && pos[1]>= 0)
                if (new_Nodes.Find(x => x.position == pos) == null)
                {
                    if (pos != start_position && pos != _pos)
                    {
                        if (checked_Nodes.Find(x => x.position == pos) == null)
                        if (pos == end_position)
                        {
                            return true;
                        }


                        Debug.Log("new neightbour " + pos[0] +" "+ pos[1] );
                        new_Nodes.Add(new Node(pos, _pos, h_owner, end_position));
                    }
                }
            }
        }

        return false;
    }


    */
}
