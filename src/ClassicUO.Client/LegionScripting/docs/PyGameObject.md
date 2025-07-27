# PyGameObject  

## Class Description
 Base class for all Python-accessible game world objects.
 Encapsulates common spatial and visual properties such as position and graphics.


## Properties
- **__class__** (*string*)
  -  The Python-visible class name of this object.
 Accessible in Python as <c>obj.__class__</c> .


- **X** (*ushort*)
  -  The X-coordinate of the object in the game world.

- **Y** (*ushort*)
  -  The Y-coordinate of the object in the game world.

- **Z** (*sbyte*)
  -  The Z-coordinate (elevation) of the object in the game world.

- **Graphic** (*ushort*)
  -  The graphic ID of the object, representing its visual appearance.

- **Hue** (*ushort*)
  -  The hue (color tint) applied to the object.


## Enums
_No enums found._

## Methods

<details><summary><h3>ToString()</h3></summary>

 Returns a readable string representation of the game object.  
 Used when printing or converting the object to a string in Python scripts.  
  

---> Return Type: *string*

</details>

***


<details><summary><h3>__repr__()</h3></summary>

 Returns a detailed string representation of the object.  
 This string is used by Python’s built-in <c>repr()</c> function.  
  

---> Return Type: *string*

</details>

***

