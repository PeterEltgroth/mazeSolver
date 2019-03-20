using dotNet.Models;

namespace dotNet.Services {
    public interface IMazeService {
        bool validateMap (string map);

        Solution solve (string map);
    }
}