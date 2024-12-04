export class CheckSessionIFrame {
  private iFrameOrigin: string;
  private iFrame: HTMLIFrameElement;
  private timeout: ReturnType<typeof setInterval> | null = null;
  private sessionState: string | null = null;

  public constructor(
    private callback: () => Promise<void>,
    private clientId: string,
    checkSessionUri: string
  ) {
    const url = new URL(checkSessionUri);
    this.iFrameOrigin = url.origin;
    this.iFrame = window.document.createElement("iframe");
    this.iFrame.style.visibility = "hidden";
    this.iFrame.style.position = "fixed";
    this.iFrame.style.left = "-1024px";
    this.iFrame.style.top = "0";
    this.iFrame.width = "0";
    this.iFrame.height = "0";
    this.iFrame.src = url.href;
    this.message = this.message.bind(this);
  }

  public setup() {
    return new Promise<void>((resolve) => {
      this.iFrame.onload = () => {
        resolve();
      };
      window.document.body.appendChild(this.iFrame);
      window.addEventListener("message", this.message, false);
    });
  }

  public start(sessionState: string) {
    if (this.sessionState === sessionState) {
      return;
    }
    this.stop();
    this.sessionState = sessionState;
    const send = () => {
      if (!this.iFrame.contentWindow) {
        return;
      }
      this.iFrame.contentWindow.postMessage(
        this.clientId + " " + this.sessionState,
        this.iFrameOrigin
      );
    };
    send();
    this.timeout = setInterval(send, 2000);
  }

  public stop() {
    this.sessionState = null;
    if (this.timeout) {
      clearInterval(this.timeout);
      this.timeout = null;
    }
  }

  private message(messageEvent: MessageEvent<string>) {
    if (
      messageEvent.origin !== this.iFrameOrigin ||
      messageEvent.source !== this.iFrame.contentWindow
    ) {
      return;
    }
    if (messageEvent.data === "error") {
      console.error("Received error from CheckSession IFrame");
      this.stop();
    } else if (messageEvent.data === "changed") {
      console.info("Received changed from CheckSession IFrame");
      this.stop();
      this.callback();
    } else {
      console.debug(`Received ${messageEvent.data} from CheckSession IFrame`);
    }
  }
}
