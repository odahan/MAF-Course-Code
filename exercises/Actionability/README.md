# Actionability: Start and End

Prerequisite: P02 structured business contracts. Read [the exercise registry](../../docs/lot-015/Exercises.md) and [contract guide](../../docs/lot-005/ContractsAndTools.md).

Goal: explain why a valid analysis is not ready for action. In a working copy of Start, call the shared draft validation twice: once for analysis and once for action. Create a new draft with an empty ClarifyingQuestions array to represent an already answered question, then validate it for action. Print all three error counts. Do not alter the shared validator, add persistence or grant permission.

Expected End output: analysis errors 0, action errors before clarification 1, action errors after clarification 0. Clearing the array in this controlled fixture is not a real user clarification conversation. Both projects compile; Start intentionally exits 2 until completed. End exits 0 after checking those three outcomes. Full invocation and automated checks are in the registry.

Prerequisites: Demo050 and Demo080; continue with Demo470. See [the course map](../../docs/CourseMap.md).
