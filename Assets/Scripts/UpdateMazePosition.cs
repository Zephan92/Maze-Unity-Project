using UnityEngine;
using System.Collections;
using System.Reflection;

public class UpdateMazePosition : MonoBehaviour {

	/*Public Global Variable
	 Current Maze: This holds the Game Controller. All blocks generated are parented to this object
	 Direction Blocks: This holds the Direction Blocks. All direction block indicaters are parented to this object
	 Flow Direction Editor: This is an array that holds which directions are flowing. It is accessible from the editor
	 Matrix Size: This is the size of the block matrix.
	 Instantiate Order: This is the amount of block generated, not including updated blocks.
	 Min Axis/Max Axis: These variables hold where the min/max coordinates are in relation to the matrix size
	 Score: This is the score
	 Path Matrix: For each (x,y) it holds 3 values. [x,y,value]
		value = 0) Holds the paths before they are moved
		value = 1) Instantiate number
		value = 2) Holds the newly updated paths after they are moved
	 Game Over: Used to check if the game is over
	 */
	public GameObject currentMaze, directionBlocks;
	public bool[] flowDirectionEditor;
	public static int matrixSize = 8, instantiateOrder = 0;
	public static float minAxis,maxAxis, score;
	public static int[,,] pathMatrix; 
	public static bool gameOver;

	/*Private Global Variables
	Destroy Count: This is a small array that holds the current destroy count of children. 
		0) Count of out of Bounds children 
		1) Count of Update Edge Children
	Child Positions: This array holds destroy children's coordinates [coordinates,child]
		child = 0) Holds the out of bounds child's coordinate
		child = 1) Holds the updated edges child's coordinate
	Player: Holds the player game object for position updates in move maze
	New Block: Holds the newly instantiated blocks briefly so they can get parented to their appropriate parent
	Flow Direction: Holds which directions the flow is currently going
		0)North
		1)East
		2)South
		3)West
	Time: This is the current time between ticks of the maze moving
	 */
	private int[] destroyCount;
	private Vector3[,] childPositions;
	private GameObject player, newBlock;
	private static bool[] flowDirection;
	private static float time;
	
	private void Start () {
		player = GameObject.Find("Player") as GameObject;//Finds the Player Game Object
		player.transform.position = Vector3.zero;//Makes sure the player start on (x,y)
		currentMaze.transform.position = Vector3.zero;//makes sure the Game Controller starts on (x,y)

		minAxis = 0;//this min x/y coordinate before leaving the matrix
		maxAxis = matrixSize - 1;//this max x/y coordinate before leaving the matrix
		score = 0;//the score always starts at zero
		time = 2;//this is the intial tick speed of the flow

		childPositions = new Vector3[matrixSize*2-1,3];//the array should always be the max possible destroy count
		pathMatrix = new int[matrixSize,matrixSize,3];//assigns the size of the matrix, 4x4 is the default
		flowDirection = new bool[4];//the array has four directions
		flowDirectionEditor = new bool[4];//the array also has four directions
		destroyCount = new int[2];//it stores 2 different destroy counts
		for(int i = 0; i < flowDirection.Length; i++)
		{//For loop to initialize the directions to all off
			flowDirection[i] = false;
			flowDirectionEditor[i] = false;
		}

		InvokeRepeating("tick", time, time);//Start the moving maze tick
		startPath();//Initializes the starting blocks
	}

	/*This function initializes the starting blocks*/
	private void startPath()
	{
		Vector2 position;
		for(int y = 0; y < matrixSize; y++)
		{
			for(int x = 0; x < matrixSize; x++)
			{
				position = new Vector2(x,y);
				instantiateNewBlock(position, true);
			}
		}
	}

	/*This function updates the game each tick.*/
	public void tick() 
	{
		updateScore();//Updates the player score
		updateTickSpeed();//Updates the tick speed
		updateFlowDirection();//Updates the flow direction
		updateMaze();//moves the maze in the flow direction
		//updatePlayerPosition();//moves the player and checks if they are out of bounds
	}

	/*Updates the tick speed depending on the player score*/
	private void updateTickSpeed()
	{//TODO Create an appropriate tick speed algorithm based on the player score
		if(score % 50 == 0)
		{
			CancelInvoke();
			//time -= 1000.0f/count;
			//if(time <= 0.5f)time = 0.5f;
			InvokeRepeating("tick", time, time);
		}
	}

	/*Updates the score depending upon various factors*/
	private void updateScore()
	{//TODO Adds some more factors for the score
		score += 100;
	}

	/*Updates the flow direction after it randomly chooses the new direction*/
	private void updateFlowDirection()
	{//TODO Update Flow Direction Randomly
		foreach(Transform child in directionBlocks.transform)
			Destroy(child.gameObject);
		flowDirection[0] = flowDirectionEditor[0];//North
		flowDirection[1] = flowDirectionEditor[1];//East
		flowDirection[2] = flowDirectionEditor[2];//South
		flowDirection[3] = flowDirectionEditor[3];//West
		if(flowDirection[0])
		{
			newBlock = Instantiate(Resources.Load("North")) as GameObject; 
			newBlock.transform.parent = directionBlocks.transform;
		}
		if(flowDirection[1])
		{
			newBlock = Instantiate(Resources.Load("East")) as GameObject; 
			newBlock.transform.parent = directionBlocks.transform;
		}
		if(flowDirection[2])
		{
			newBlock = Instantiate(Resources.Load("South")) as GameObject; 
			newBlock.transform.parent = directionBlocks.transform;
		}
		if(flowDirection[3])
		{
			newBlock = Instantiate(Resources.Load("West")) as GameObject; 
			newBlock.transform.parent = directionBlocks.transform;
		} 
	}

	/*Updates the maze in the direction of the flow
	1) it moves each child in the flow direction
	2) if it checks if it is out of bounds, if so save the data and then delete it
	3) else check to see if it is on the edge of the flow, if so delete it and the save the data
	4) Make new children at any empty spots farthest from the flow
	5) Make new children at flow edges
	6) Update the matrix with the current maze paths
	 */
	private void updateMaze()
	{
		int x,y;//used for position checking
		destroyCount[0] = 0;//each tick resets the amount of out of bounds children were destroyed to 0
		destroyCount[1] = 0;//each tick resets the amount of edge updates children were destroyed to 0
		foreach(Transform child in currentMaze.transform)//Checks
		{
			int tempPathHolder = pathMatrix[(int) child.position.x,y = (int) child.position.y,0];//save child path
			child.position = child.position + getFlowDirectionVector();//update child position
			x = (int) child.position.x;//save child x coord
			y = (int) child.position.y;//save child y coord
			
			if((flowDirection[0] && y > maxAxis) || //North
			   (flowDirection[1] && x > maxAxis) || //East
			   (flowDirection[2] && y < minAxis) || //South
			   (flowDirection[3] && x < minAxis))	//West
			{//if out of bounds delete the child after saving the data
				pathMatrix[(int)updatePosition(child.position).x,(int)updatePosition(child.position).y,1] = -1;//remove path number
				childPositions[destroyCount[0],0] = child.position;//save child position
				Destroy(child.gameObject);//destroy child
				destroyCount[0]++;//count how many children have been destroyed
			}
			else//All other Children
			{
				pathMatrix[x,y,2] = tempPathHolder;//update path number
				if((flowDirection[0] && y == maxAxis) || //North
				   (flowDirection[1] && x == maxAxis) || //East
				   (flowDirection[2] && y == minAxis) || //South
				   (flowDirection[3] && x == minAxis))   //West
				{//if child is on the edge of the flow
					childPositions[destroyCount[1],1] = child.position;//save child position
					Destroy(child.gameObject);//destroy child
					destroyCount[1]++;//count how many children have been destroyed
				}
			}
		}
		
		for(int i=0; i < destroyCount[0]; i++)//loop to reinstantiate out of bounds children
			instantiateNewBlock(childPositions[i,0], false);//Update Out of Bounds Children

		for(int i=0; i < destroyCount[1]; i++)//loop to reinstantiate edge flow children
		{
			x = (int)childPositions[i,1].x;
			y = (int)childPositions[i,1].y;
			updateEdgeBlock(childPositions[i,1],pathMatrix[x,y,2]);//update the path edge
			newBlock = Instantiate(Resources.Load(returnPath(pathMatrix[x,y,2])),//instantiate new on
			                       childPositions[i,1],Quaternion.identity) as GameObject; 
			newBlock.transform.parent = currentMaze.transform;//make the sure the parent is game controller
		}
		
		for(int i=0; i < matrixSize; i++)//loop to update the matrix to current paths
			for(int j=0; j < matrixSize; j++)
				pathMatrix[j,i,0] = pathMatrix[j,i,2];//copy over the new paths over the old paths
	}

	/*Updates the player position depending upon the flow
	 also checks to see if the player went out of bounds, if so game over*/
	private void updatePlayerPosition()
	{
		player.transform.position = player.transform.position + getFlowDirectionVector();//update player position
		if(player.transform.position.x < minAxis || // west
		   player.transform.position.y < minAxis || // south
		   player.transform.position.x > maxAxis || // east
		   player.transform.position.y > maxAxis)   // north
		{//if out of bounds stop the flow tick
			CancelInvoke();//cancel flow
			Debug.Log ("Game Over");
			Debug.Log ("Your score is: " + score);
			gameOver = true;
		}
	}

	/*Function that returns the current flow direction vector*/
	private Vector3 getFlowDirectionVector()
	{
		int x = 0;
		int y = 0;
		if(flowDirection[0])y += 1;//North
		if(flowDirection[1])x += 1;//East
		if(flowDirection[2])y += -1;//South
		if(flowDirection[3])x += -1;//West
		return new Vector3(x,y,0);
	}

	/*Updates the path number argument to not include paths in the direction of the flow*/
	private void updateEdgeBlock(Vector3 position, int pathNumber)
	{
		int x = (int)position.x;
		int y = (int)position.y;

		if(flowDirection[0] && y == maxAxis)//Update North Edge
		{
			pathNumber = UpdateNorthPath(pathNumber);
		}
		if(flowDirection[1] && x == maxAxis)//Update East Edge
		{
			pathNumber = UpdateEastPath(pathNumber);
		}
		if(flowDirection[2] && y == minAxis)//Update South Edge
		{
			pathNumber = UpdateSouthPath(pathNumber);
		}
		if(flowDirection[3] && x == minAxis)//Update West Edge
		{
			pathNumber = UpdateWestPath(pathNumber);
		}
		pathMatrix[x,y,2] = pathNumber;
	}

	/*instantiate's a new block at the specified position*/
	private void instantiateNewBlock (Vector3 position, bool start)
	{
		if(!start)
		{	
			position = updatePosition(position);//if this is the start the position stays the same
		}
		int x = (int)position.x;
		int y = (int)position.y;
		pathMatrix[x,y,1] = instantiateOrder;//save the instantiate number
		instantiateOrder++;//update the number
		pathMatrix[x,y,2] = choosePathNumber(position, start);//randomly generates a path depending on the paths around it
		if(start)
		{	
			pathMatrix[x,y,0] = pathMatrix[x,y,2];//if this is the start, inialize the path matrix
		}
		string pathString = returnPath(pathMatrix[x,y,2]);//returns the correct string for the path number
		newBlock = Instantiate(Resources.Load(pathString),position,Quaternion.identity) as GameObject;//instantiate the path
		newBlock.transform.parent = currentMaze.transform;//make sure the parent is game controller
	}

	/*for out of bounds paths, this updates where the position will spawn the next path*/
	private Vector3 updatePosition(Vector3 position)
	{
		int x = (int)position.x;
		int y = (int)position.y;
		if(x < minAxis)//if deleted outside west
			position = position + new Vector3(matrixSize,0,0);
		if(x > maxAxis)//if deleted outside east
			position = position + new Vector3(-matrixSize,0,0);
		if(y < minAxis)//if deleted outside south
			position = position + new Vector3(0,matrixSize,0);
		if(y > maxAxis)//if deleted outside north
			position = position + new Vector3(0,-matrixSize,0);
		return position;
	}

	/*Randomly chooses the path number based on the spaces around it*/
	int choosePathNumber(Vector2 position, bool start)
	{
		int x = (int)position.x;
		int y = (int)position.y;
		int pathNumber = 0;

		if(start)//TODO CONSIDER OTHER STARTING PATHS
		{
			if(x == 0 && y == 0)//if this is the SW corner
			{
				pathNumber = Random.Range(1,4);
			}
			else if(checkSouth(position) && checkWest(position))//check your south and west and if they have paths connect them
			{
				pathNumber = Random.Range(13,16);
			}
			else if(checkWest(position))//checks the west
			{
				pathNumber = Random.Range(9,12);
			}
			else if(checkSouth(position))//checks the east
			{
				pathNumber = Random.Range(5,8);
			}
			else//only paths that don't connect to anything to the west or east
			{
				pathNumber = Random.Range(1,4);
			}
		}
		else//if not start generate the path depending on all four directions
		{
			bool[] neswArray = new bool[4];//holds which directions it should consider before generating a path
			string nesw = "";

			if(y < maxAxis && pathMatrix[x,y+1,1] != -1)//north
			{
				neswArray[0] = true;
				nesw += "N";
			}
			if(x < maxAxis && pathMatrix[x+1,y,1] != -1)//east
			{
				neswArray[1] = true;
				nesw += "E";
			}
			if(y > minAxis && pathMatrix[x,y-1,1] != -1)//south
			{
				neswArray[2] = true;
				nesw += "S";
			}
			if(x > minAxis && pathMatrix[x-1,y,1] != -1)//west
			{
				neswArray[3] = true;
				nesw += "W";
			} 
			if(!nesw.Equals(""))//if there are connections available
				pathNumber = invokeChoosePath(position,nesw);//choose a connecting path
			else//else spawn anything but a complete dead end
				pathNumber = Random.Range(1,16);

			//check all paths to make sure you don't have any broken paths, if you do update them
			if(!checkNorth(position)&& neswArray[0] || flowDirection[0] && y == maxAxis)//north
				pathNumber = UpdateNorthPath(pathNumber);
			if(!checkEast(position) && neswArray[1] || flowDirection[1] && x == maxAxis)//east
				pathNumber = UpdateEastPath(pathNumber);
			if(!checkSouth(position)&& neswArray[2] || flowDirection[2] && y == minAxis)//south
				pathNumber = UpdateSouthPath(pathNumber);
			if(!checkWest(position) && neswArray[3] || flowDirection[3] && x == minAxis)//west
				pathNumber = UpdateWestPath(pathNumber);
		}
		return pathNumber;
	}

	/*This function takes a string and function and calls the function that corrensponds with the string*/
	private int invokeChoosePath(Vector2 position, string nesw)
	{
		string pass = "choosePath" + nesw;//append which directions choose path should look in to the string
		object[] array = new object[1];//arguments
		array[0] = position;//assigns the argument, position
		MethodInfo path = this.GetType().GetMethod(pass);//this puts a method in the variable "path"
		return (int) path.Invoke(this, array);//this invokes the method in "path", which does the choosePath%%%%(position) function
	}

	/*returns a random path based on NESW*/
	public static int choosePathNESW(Vector2 position)
	{
		return 15;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathNES(Vector2 position)
	{
		int switchNumber = Random.Range(0,2), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 7;
			break;
		case 1:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathNEW(Vector2 position)
	{
		int switchNumber = Random.Range(0,2), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 11;
			break;
		case 1:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathNSW(Vector2 position)
	{
		int switchNumber = Random.Range(0,2), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 13;
			break;
		case 1:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathESW(Vector2 position)
	{
		int switchNumber = Random.Range(0,2), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 14;
			break;
		case 1:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathNE(Vector2 position)
	{
		int switchNumber = Random.Range(0,4), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 3;
			break;
		case 1:
			pathNumber = 7;
			break;
		case 2:
			pathNumber = 11;
			break;
		case 3:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathNW(Vector2 position)
	{
		int switchNumber = Random.Range(0,4), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 9;
			break;
		case 1:
			pathNumber = 11;
			break;
		case 2:
			pathNumber = 13;
			break;
		case 3:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathNS(Vector2 position)
	{
		int switchNumber = Random.Range(0,4), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 5;
			break;
		case 1:
			pathNumber = 7;
			break;
		case 2:
			pathNumber = 13;
			break;
		case 3:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathES(Vector2 position)
	{
		int switchNumber = Random.Range(0,4), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 6;
			break;
		case 1:
			pathNumber = 7;
			break;
		case 2:
			pathNumber = 14;
			break;
		case 3:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathEW(Vector2 position)
	{
		int switchNumber = Random.Range(0,4), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 10;
			break;
		case 1:
			pathNumber = 11;
			break;
		case 2:
			pathNumber = 14;
			break;
		case 3:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}

	/*returns a random path based on NESW*/
	public static int choosePathSW(Vector2 position)
	{
		int switchNumber = Random.Range(0,4), pathNumber = 0;
		switch(switchNumber)
		{	
		case 0:
			pathNumber = 12;
			break;
		case 1:
			pathNumber = 13;
			break;
		case 2:
			pathNumber = 14;
			break;
		case 3:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathN(Vector2 position)
	{
		int switchNumber = Random.Range(0,8), pathNumber = 0;
		switch(switchNumber)
		{
		case 0:
			pathNumber = 1;
			break;
		case 1:
			pathNumber = 3;
			break;
		case 2:
			pathNumber = 5;
			break;
		case 3:
			pathNumber = 7;
			break;
		case 4:
			pathNumber = 9;
			break;
		case 5:
			pathNumber = 11;
			break;
		case 6:
			pathNumber = 13;
			break;
		case 7:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathE(Vector2 position)
	{
		int switchNumber = Random.Range(0,8), pathNumber = 0;
		switch(switchNumber)
		{
		case 0:
			pathNumber = 2;
			break;
		case 1:
			pathNumber = 3;
			break;
		case 2:
			pathNumber = 6;
			break;
		case 3:
			pathNumber = 7;
			break;
		case 4:
			pathNumber = 10;
			break;
		case 5:
			pathNumber = 11;
			break;
		case 6:
			pathNumber = 14;
			break;
		case 7:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathS(Vector2 position)
	{
		int switchNumber = Random.Range(0,8), pathNumber = 0;
		switch(switchNumber)
		{
		case 0:
			pathNumber = 4;
			break;
		case 1:
			pathNumber = 5;
			break;
		case 2:
			pathNumber = 6;
			break;
		case 3:
			pathNumber = 7;
			break;
		case 4:
			pathNumber = 12;
			break;
		case 5:
			pathNumber = 13;
			break;
		case 6:
			pathNumber = 14;
			break;
		case 7:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePathW(Vector2 position)
	{
		int switchNumber = Random.Range(0,8), pathNumber = 0;
		switch(switchNumber)
		{
		case 0:
			pathNumber = 8;
			break;
		case 1:
			pathNumber = 9;
			break;
		case 2:
			pathNumber = 10;
			break;
		case 3:
			pathNumber = 11;
			break;
		case 4:
			pathNumber = 12;
			break;
		case 5:
			pathNumber = 13;
			break;
		case 6:
			pathNumber = 14;
			break;
		case 7:
			pathNumber = 15;
			break;
		}
		return pathNumber;
	}
	
	/*returns a random path based on NESW*/
	public static int choosePath(Vector2 position)
	{
		return 0;
	}
	
	/*Checks the west for a path, if yes returns true*/
	public static  bool checkWest(Vector2 position)
	{
		int x = (int)position.x;
		int y = (int)position.y;
		if(x <= minAxis)
			return false;

		if(pathMatrix[x-1,y,2] == 0)
			return false;

		if(pathMatrix[x-1,y,2] == 2 || 
		   pathMatrix[x-1,y,2] == 3 ||
		   pathMatrix[x-1,y,2] == 6 ||
		   pathMatrix[x-1,y,2] == 7 ||
		   pathMatrix[x-1,y,2] == 10 ||
		   pathMatrix[x-1,y,2] == 11 ||
		   pathMatrix[x-1,y,2] == 14 ||
		   pathMatrix[x-1,y,2] == 15)
			return true;

		return false;
	}

	/*Checks the south for a path, if yes returns true*/
	public static bool checkSouth(Vector2 position)
	{
		int x = (int)position.x;
		int y = (int)position.y;
		if(y <= minAxis)
			return false;

		if(pathMatrix[x,y-1,2] == 0)
			return false;

		if(pathMatrix[x,y-1,2] == 1 || 
		   pathMatrix[x,y-1,2] == 3 ||
		   pathMatrix[x,y-1,2] == 5 ||
		   pathMatrix[x,y-1,2] == 7 ||
		   pathMatrix[x,y-1,2] == 9 ||
		   pathMatrix[x,y-1,2] == 11 ||
		   pathMatrix[x,y-1,2] == 13 ||
		   pathMatrix[x,y-1,2] == 15)
			return true;

		return false;
	}

	/*Checks the North for a path, if yes returns true*/
	public static  bool checkNorth(Vector2 position)
	{
		int x = (int)position.x;
		int y = (int)position.y;
		if(y >= maxAxis)
			return false;
		
		if(pathMatrix[x,y+1,2] == 0)
			return false;

		if(pathMatrix[x,y+1,2] == 4 || 
		   pathMatrix[x,y+1,2] == 5 ||
		   pathMatrix[x,y+1,2] == 6 ||
		   pathMatrix[x,y+1,2] == 7 ||
		   pathMatrix[x,y+1,2] == 12 ||
		   pathMatrix[x,y+1,2] == 13 ||
		   pathMatrix[x,y+1,2] == 14 ||
		   pathMatrix[x,y+1,2] == 15)
			return true;

		return false;
	}

	/*Checks the East for a path, if yes returns true*/
	public static  bool checkEast(Vector2 position)
	{
		int x = (int)position.x;
		int y = (int)position.y;
		if(x >= maxAxis)
			return false;
		
		if(pathMatrix[x+1,y,2] == 0)
			return false;
		
		if(pathMatrix[x+1,y,2] == 8 || 
		   pathMatrix[x+1,y,2] == 9 ||
		   pathMatrix[x+1,y,2] == 10 ||
		   pathMatrix[x+1,y,2] == 11 ||
		   pathMatrix[x+1,y,2] == 12 ||
		   pathMatrix[x+1,y,2] == 13 ||
		   pathMatrix[x+1,y,2] == 14 ||
		   pathMatrix[x+1,y,2] == 15)
			return true;
		
		return false;
	}

	/*Returns the corrensponding path name for the given number*/
	private string returnPath(int pathNumber)
	{
		switch(pathNumber)
		{
		case 0:
			return "D_D";
		case 1:
			return "D_N";
		case 2:
			return "D_E";
		case 3:
			return "D_NE";
		case 4:
			return "S_D";
		case 5:
			return "S_N";
		case 6:
			return "S_E";
		case 7:
			return "S_NE";
		case 8:
			return "W_D";
		case 9:
			return "W_N";
		case 10:
			return "W_E";
		case 11:
			return "W_NE";
		case 12:
			return "SW_D";
		case 13:
			return "SW_N";
		case 14:
			return "SW_E";
		case 15:
			return "SW_NE";
		default:
			return "D_D";
		}
	}

	/*Removes any path numbers that have north in them*/
	private int UpdateNorthPath(int pathNumber)
	{
		switch(pathNumber)
		{
		case 0:
		case 1:
			pathNumber = 0;//D_D
			break;
		case 2:
		case 3:
			pathNumber = 2;//D_E
			break;
		case 4:
		case 5:
			pathNumber = 4;//S_D
			break;
		case 6:
		case 7:
			pathNumber = 6;//S_E
			break;
		case 8:
		case 9:
			pathNumber = 8;//W_D
			break;
		case 10:
		case 11:
			pathNumber = 10;//W_E
			break;
		case 12:
		case 13:
			pathNumber = 12;//SW_D
			break;
		case 14:
		case 15:
			pathNumber = 14;//SW_E
			break;
		}
		return pathNumber;
	}

	/*Removes any path numbers that have west in them*/
	private int UpdateWestPath(int pathNumber)
	{
		switch(pathNumber)
		{
		case 0:
		case 8:
			pathNumber = 0;//D_D
			break;
		case 1:
		case 9:
			pathNumber = 1;//D_N
			break;
		case 2:
		case 10:
			pathNumber = 2;//D_E
			break;
		case 3:
		case 11:
			pathNumber = 3;//D_NE
			break;
		case 4:
		case 12:
			pathNumber = 4;//S_D
			break;
		case 5:
		case 13:
			pathNumber = 5;//S_NE
			break;
		case 6:
		case 14:
			pathNumber = 6;//S_E
			break;
		case 7:
		case 15:
			pathNumber = 7;//S_N
			break;
		}
		return pathNumber;
	}

	/*Removes any path numbers that have east in them*/
	private int UpdateEastPath(int pathNumber)
	{
		switch(pathNumber)
		{
		case 0:
		case 2:
			pathNumber = 0;//D_D
			break;
		case 1:
		case 3:
			pathNumber = 1;//D_N
			break;
		case 4:
		case 6:
			pathNumber = 4;//S_D
			break;
		case 5:
		case 7:
			pathNumber = 5;//S_N
			break;
		case 8:
		case 10:
			pathNumber = 8;//W_D
			break;
		case 9:
		case 11:
			pathNumber = 9;//W_N
			break;
		case 12:
		case 14:
			pathNumber = 12;//SW_D
			break;
		case 13:
		case 15:
			pathNumber = 13;//SW_N
			break;
		}
		return pathNumber;
	}

	/*Removes any path numbers that have south in them*/
	private int UpdateSouthPath(int pathNumber)
	{
		switch(pathNumber)
		{
		case 0:
		case 4:
			pathNumber = 0;//D_D
			break;
		case 1:
		case 5:
			pathNumber = 1;//D_N
			break;
		case 2:
		case 6:
			pathNumber = 2;//D_E
			break;
		case 3:
		case 7:
			pathNumber = 3;//D_NE
			break;
		case 8:
		case 12:
			pathNumber = 8;//W_D
			break;
		case 9:
		case 13:
			pathNumber = 9;//W_NE
			break;
		case 10:
		case 14:
			pathNumber = 10;//W_E
			break;
		case 11:
		case 15:
			pathNumber = 11;//W_N
			break;
		}
		return pathNumber;
	}
}//End of Update Maze Position
