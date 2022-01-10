using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public int[] position;
    int[] owner;

    int g_Value; // this -> target
    public int h_Value; // this -> owner
    
    public Node(int[] _position, int[] _owner, int _hOwner, int[] target)
    {
        position = _position;
        owner = _owner;

        if(_position[0] != _owner[0] && _position[1] != _owner[1])
        {
            h_Value = _hOwner += 14;
        }
        else
        {
            h_Value = _hOwner += 10;
        }

        int dystX = Mathf.Abs(position[0] - target[0]);
        int dystY = Mathf.Abs(position[1] - target[1]);

        if(dystX > dystY)
        {
            g_Value = 14 * dystY + 10 * (dystX - dystY);
        }
        else
        {
            g_Value = 14 * dystX + 10 * (dystY - dystX);
        }

    }
     
    public int f_Value
    {
        get
        {
            return g_Value + h_Value;
        }
        
    }
    

}
