﻿using UnityEngine;
using System.Collections.Generic;

/* Container.
 * 
 * Details:
 *  A container is, quite literally, a rectangle in a 2D plane. You can ask it for anything
 *  a rectangle can possibly have/do. This includes:
 *      1. Left, Right, Top, and Bottom bounds
 *      2. Position and Dimension
 *      3. Width and Height
 *      4. Center
 *      5. TL TR BL BR corners
 *  
 *  You can:
 *      1. Join two containers of the same depth together.
 *      2. Split a container vertically
 *      3. Split a container horizontally
 *      4. Check if two containers overlap
 *  
 *  The recursive quarter algorithm is built into the Container. Split details come out as a
 *  ContainerSplitPacket struct.
 *  
 *  
 * Technicals:
 *  A container can have a position relative to another container. That is to say, a Container
 *  has an owner, and we can ask of its position relative to owner's position. To manage this,
 *  we include a 'Depth' parameter. To get a Container's position, you must first move it to
 *  a desired depth.
 *  
 * 
 * Note:
 *  A Container's position at its own type's depth is (0,0).
 *  
 * auth Wesley Wu
 */


/// <summary>
/// A class like Unity's Rect, but better.
/// </summary>
public class Container
{
    public Container owner;
    public Depth depth;
    private int left;
    private int right;
    private int top;
    private int bottom;

    public int width { get { return this.right - this.left; } }
    public int height { get { return this.top - this.bottom; } }
    
    private Point relative_position;
    public Point position_at_current_depth
    {
        get
        {
            return new Point(this.left, this.bottom);
        }
        set
        {
            Point diff = position_at_current_depth - value;
            this.left -= diff.x;
            this.right -= diff.x;
            this.top -= diff.y;
            this.bottom -= diff.y;
        }
    }
    public Point dimension { get { return new Point(this.right - this.left, this.top - this.bottom); } }


    public Container() { }

    public Container(Container container)
        : this(container.owner, container.relative_position, container.position_at_current_depth, container.dimension) { }

    public Container(Container owner, int left, int right, int top, int bottom)
        : this(owner, new Point(left, bottom), new Point(right - left, top - bottom)) { }
    
    public Container(Container owner, Point relative_position, Point dimension)
        : this(owner, relative_position, Point.zero, dimension) { }
    
    public Container(Container owner, Point relative_position, Point position, Point dimension)
    {
        this.owner = owner;
        this.depth = owner == null ? 0 : (Depth)((int)owner.depth + 1);
        this.relative_position = relative_position;
        this.left = (int)position.x;
        this.right = (int)(position.x + dimension.x);
        this.top = (int)(position.y + dimension.y);
        this.bottom = (int)position.y;
    }

    public Point Position(Depth depth)
    {
        return this.ToDepth(depth).position_at_current_depth;
    }

    public int Left(Depth depth)
    {
        return this.ToDepth(depth).left;
    }

    public int Right(Depth depth)
    {
        return this.ToDepth(depth).right;
    }

    public int Top(Depth depth)
    {
        return this.ToDepth(depth).top;
    }

    public int Bottom(Depth depth)
    {
        return this.ToDepth(depth).bottom;
    }

    private Container ToDepth(Depth depth)
    {
        Container to_return = new Container(this);
        while (to_return.depth > depth)
            to_return = to_return.MinusDepth();
        return to_return;
    }

    private Container MinusDepth()
    {
        if ((int)this.depth == 0)
            return this;
        return new Container(this.owner.owner, this.owner.relative_position, this.position_at_current_depth + this.relative_position, this.dimension);
    }


    /// <summary>
    /// Join two containers.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Container Join(Container other)
    {
        if (this.depth != other.depth)
            Debug.LogError("Joining two Containers of different depth! " + this.depth + " and " + other.depth);
        return new Container(this.owner,
                                Mathf.Min(this.Left(depth - 1), other.Left(depth - 1)),
                                Mathf.Max(this.Right(depth - 1), other.Right(depth - 1)),
                                Mathf.Max(this.Top(depth - 1), other.Top(depth - 1)),
                                Mathf.Min(this.Bottom(depth - 1), other.Bottom(depth - 1)));
    }

    /// <summary>
    /// Returns true if two containers overlap.
    /// </summary>
    /// <param name="mine"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Overlaps(Container other)
    {
        if (this.right < other.left || other.right < this.left || this.top < other.bottom || other.top < this.bottom)
            return false;
        return true;
    }

    public override string ToString()
    {
        string to_return = "Depth-Info(" + depth + ")";
        to_return += "\n Position - " + position_at_current_depth + ", Dimension - " + dimension;
        to_return += "\n L" + left + " R" + right + " T" + top + " B" + bottom;
        return to_return;
    }
}
