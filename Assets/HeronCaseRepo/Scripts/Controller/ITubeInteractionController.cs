using System.Collections.Generic;

public interface ITubeInteractionController
{
    void Initialize(List<TubeView> tubes);
    void OnTubeClicked(TubeView tube);
    void OnEmptySpaceClicked();
}
