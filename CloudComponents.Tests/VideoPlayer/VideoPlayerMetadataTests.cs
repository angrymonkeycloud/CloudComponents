using global::CloudComponents.VideoPlayer;

namespace CloudComponents.Tests.VideoPlayer;

public class VideoPlayerMetadataTests
{
    [Fact]
    public void A_new_metadata_describes_a_stopped_player_with_all_controls_on()
    {
        VideoPlayerMetadata metadata = new();

        Assert.Null(metadata.VideoUrl);
        Assert.Null(metadata.Title);
        Assert.Equal(1, metadata.Volume);
        Assert.True(metadata.EnableControls);
        Assert.True(metadata.ShowSettings);
        Assert.True(metadata.ShowProgressBar);
        Assert.False(metadata.ShowStopButton);
        Assert.False(metadata.Autoplay);
        Assert.False(metadata.Loop);
        Assert.False(metadata.IsLive);
        Assert.False(metadata.ReserveAspectRatio);
        Assert.Equal(VolumeType.Full, metadata.VolumeType);
        Assert.Equal(VideoStates.NoVideo, metadata.VideoState);
        Assert.Equal(PlayingStates.NotPlaying, metadata.PlayingState);
        Assert.False(metadata.IsPlayingState);
        Assert.False(metadata.IsCasting);
        Assert.Equal(0, metadata.CurrentTime);
    }

    [Fact]
    public void Playing_marks_the_player_as_playing()
    {
        VideoPlayerMetadata metadata = new() { PlayingState = PlayingStates.Playing };

        Assert.True(metadata.IsPlayingState);
    }

    [Fact]
    public void Stopping_clears_the_playing_flag()
    {
        VideoPlayerMetadata metadata = new() { PlayingState = PlayingStates.Playing };

        metadata.PlayingState = PlayingStates.NotPlaying;

        Assert.False(metadata.IsPlayingState);
    }

    [Theory]
    [InlineData(VideoStates.Loading)]
    [InlineData(VideoStates.Ready)]
    [InlineData(VideoStates.Error)]
    public void Changing_the_video_state_resets_the_playing_flag(VideoStates next)
    {
        VideoPlayerMetadata metadata = new() { PlayingState = PlayingStates.Playing };

        metadata.VideoState = next;

        Assert.Equal(next, metadata.VideoState);
        Assert.False(metadata.IsPlayingState);
    }

    [Fact]
    public void Setting_the_same_video_state_again_does_not_reset_the_playing_flag()
    {
        VideoPlayerMetadata metadata = new() { VideoState = VideoStates.Ready, PlayingState = PlayingStates.Playing };

        metadata.VideoState = VideoStates.Ready;

        Assert.True(metadata.IsPlayingState);
    }

    [Fact]
    public void The_enums_keep_their_numeric_values_for_hosts_that_store_them()
    {
        Assert.Equal([0, 1, 2, 3], Enum.GetValues<VideoStates>().Select(value => (int)value));
        Assert.Equal([0, 1, 2, 3], Enum.GetValues<PlayingStates>().Select(value => (int)value));
        Assert.Equal(["Full", "Mute", "None"], Enum.GetNames<VolumeType>());
    }
}
