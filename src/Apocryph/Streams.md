| Stream Name        | Returning type                            |   |   |   |
|--------------------|-------------------------------------------|---|---|---|
| agentMessageStream | IAsyncEnumerable\<AgentMessage>           |   |   |   |
| kothStates         | IAsyncEnumerable\<(Hash<Chain>, Slot?[])> |   |   |   |
| calls          | IAsyncEnumerable\<AgentMessage>           |   |   |   |
| subscriptions          | IAsyncEnumerable\<List<AgentReference>>   |   |   |   |
| outbox          | IAsyncEnumerable\<AgentMessage>   |   |   |   |
| collector          | IAsyncEnumerable\<AgentMessage>   |   |   |   |
