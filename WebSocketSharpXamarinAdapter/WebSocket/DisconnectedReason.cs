namespace WebSocketSharpXamarinAdapter.WebSocket
{
    public enum DisconnectedReason
    {
        /// <summary>
        /// User closed connection on log out
        /// </summary>
        User,
        /// <summary>
        /// Should be reconnected on entering foreground
        /// </summary>
        Background,
        /// <summary>
        /// Should be reconnected after BGD ends
        /// </summary>
        BGD,
        /// <summary>
        /// Have to repeat Authorization process
        /// </summary>
        AuthError,
        /// <summary>
        /// Have to attempt reconnection periodically by timer
        /// </summary>
        Unknown
    }
}