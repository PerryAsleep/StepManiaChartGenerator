# Step Types
The application understands the following StepTypes. Jumps are represented internally as one StepType per foot at the same time.

### Simple Steps
- **SameArrow**: The foot steps on an arrow it is currently resting on.
- **NewArrow**: The foot steps on a new, unoccupied arrow.
- **CrossoverFront**: The foot steps on a new arrow, crossing over in front of the other foot.
- **CrossoverBehind**: The foot steps on a new arrow, crossing over in back of the other foot.
- **InvertFront**: (a.k.a. Afronova walk) The foot steps on a new arrow, crossing over in front of the other foot and ending in an orientation facing away from the machine. On singles this would be left foot on the right arrow and right foot on the left arrow.
- **InvertBehind**: (a.k.a. Afronova walk) The foot steps on a new arrow, crossing over in back of the other foot and ending in an orientation facing away from the machine. On singles this would be left foot on the right arrow and right foot on the left arrow.
- **FootSwap**: The foot steps on a new arrow that the other foot was resting on.

### Brackets: Stepping on two arrows with one foot
- **BracketHeelNewToeNew**: The foot steps on two arrows where both the heel and toe portion of the foot move to unoccupied new arrows.
- **BracketHeelNewToeSame**: The foot steps on two arrows where the heel portion steps on an unoccuppied new arrow and the toe portion steps on an arrow that the foot was already resting on.
- **BracketHeelSameToeNew**: The foot steps on two arrows where the heel portion steps on an arrow that the foot was already resting on and the toe portion steps on an unoccuppied new arrow.
- **BracketHeelSameToeSame**: The foot steps on two arrows where both the heel and toe portion of the foot step on arrows that the foot was already resting on.

### Brackets: Stepping on two arrows with one foot while performing a FootSwap
- **BracketHeelSameToeSwap**: The foot steps on two arrows where the heel portion steps on an arrow that the foot was already resting on and the toe portion steps on an arrow that the other foot was resting on.
- **BracketHeelNewToeSwap**: The foot steps on two arrows where the heel portion steps on an unoccuppied new arrow and the toe portion steps on an arrow that the other foot was resting on.
- **BracketHeelSwapToeSame**: The foot steps on two arrows where the heel portion steps on an arrow that the other foot was resting on and the toe portion steps on an arrow that the foot was already resting on.
- **BracketHeelSwapToeNew**: The foot steps on two arrows where the heel portion steps on an arrow that the other foot was resting on and the toe portion steps on an unoccuppied new arrow.

### Brackets: Stepping on one arrow with a foot that is already holding another on another arrow
- **BracketOneArrowHeelSame**: While the toe portion of the foot is holding, the heel portion steps on an arrow that the foot was already resting on.
- **BracketOneArrowHeelNew**: While the toe portion of the foot is holding, the heel portion steps on an unoccuppied new arrow.
- **BracketOneArrowToeSame**: While the heel portion of the foot is holding, the toe portion steps on an arrow that the foot was already resting on.
- **BracketOneArrowToeNew**: While the heel portion of the foot is holding, the toe portion steps on an unoccuppied new arrow.