using System;
using Unity.WebRTC;

// TODO: implement proper await handlers 
public static class AsyncExtensions
{
    public static AsyncOperationBase AwaitOperation(AsyncOperationBase operation)
    {
        while (!operation.IsDone && !operation.IsError) { }

        if (operation.IsError)
        {
            throw new Exception(operation.Error.message);
        }

        return operation;
    }
}

