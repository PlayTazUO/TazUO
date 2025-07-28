# PyEntity  

## Class Description
 Represents a Python-accessible entity in the game world, such as a mobile or item.
 Inherits basic spatial and visual data from <see cref="PyGameObject"/> .


## Properties
- **Distance** (*int*)
- **Name** (*string*)
- **__class__** (*string*)
  -  The Python-visible class name of this object.
 Accessible in Python as <c>obj.__class__</c> .


- **Serial** (*uint*)
  -  The unique serial identifier of the entity.


## Enums
_No enums found._

## Methods

<details><summary><h3>ToString()</h3></summary>

 Returns a readable string representation of the entity.  
 Used when printing or converting the object to a string in Python scripts.  
  

---> Return Type: *string*

</details>

***


<details><summary><h3>SetHue(hue)</h3></summary>

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| hue | ushort | No |  |

---> Does not return anything

</details>

***

