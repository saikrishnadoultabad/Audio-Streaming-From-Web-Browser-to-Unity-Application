import * as Logger from "../../module/logger.js";

export class SendVideo {
  constructor(localVideoElement) {
    this.localVideo = localVideoElement;
  }

  /**
   * @param {MediaTrackConstraints} videoSource
   * @param {MediaTrackConstraints} audioSource
   * @param {number} videoWidth
   * @param {number} videoHeight
   */
  async startLocalVideo(audioSource) {
    try {
      const constraints = {
        //video: true,
        audio: { deviceId: audioSource ? { exact: audioSource } : undefined }
      };



      const localStream = await navigator.mediaDevices.getUserMedia(constraints);
      this.localVideo.srcObject = localStream;
      console.log(localStream)
      await this.localVideo.play();
      console.log(localStream)
    } catch (err) {
      Logger.error(`mediaDevice.getUserMedia() error:${err}`);
    }
  }

  /**
   * @returns {MediaStreamTrack[]}
   */
  getLocalTracks() {
    return this.localVideo.srcObject.getTracks();
  }

  /**
   * @param {MediaStreamTrack} track
   */
  addRemoteTrack(track) {
    if (this.remoteVideo.srcObject == null) {
      this.remoteVideo.srcObject = new MediaStream();
    }
    this.remoteVideo.srcObject.addTrack(track);
  }
}
