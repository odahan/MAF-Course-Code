# Exercise: isolate the candidate from the expected result

Goal: prove that reference edits cannot change what the candidate receives.

1. Run Episode17Demo.Tests and inspect CandidateReceivesNoPrivateExpectationsAndJudgeGetsOriginalAnswer.
2. In a temporary copy of an evaluation case, change its private ID, required/forbidden criteria, reference and expected action. Keep CandidateInput unchanged.
3. Capture the candidate runner's instructions and input. Verify that the input remains exactly the serialized public UserInput/Context projection.
4. Capture the judge input. Verify that criteria and the original candidate answer appear there, and that no answer field was replaced by the expected value.
5. Return an answer with a missing Refused property, then an incomplete judge verdict. Both must be ContractError, with the correct stage. No fabricated default may produce a pass.
6. Keep many successful ordinary cases and insert one failing critical case. The campaign must reject. Repeat with a critical contract error and an incomplete run.
7. Run fixture mode, then live mode when Ollama is available. Explain why a passing scripted fixture says nothing about the model's quality.

Solution evidence: EvaluationTests verifies isolation, unchanged answers, malformed contracts, incomplete runs and independent blocking criteria; ModelRunnerTests verifies native schema/reader agreement and fixture consistency. The observed calibration in this lot also demonstrates that a judge can be fooled by a refusal flag while the prose violates the policy. ModelGatePassed remains false.

Never edit the historical archives or use actual personal information for this exercise.

