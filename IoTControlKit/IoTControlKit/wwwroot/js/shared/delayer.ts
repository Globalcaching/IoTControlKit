// Simple delayer to call functions not too many times in a certain timespan.
export class Delayer {
    timespan = null; // timespan in milliseconds
    lastTime = null; // last time executed in milliseconds since 1970/01/01
    functionHolder = null;
    constructor(timespan: any) {
        this.timespan = timespan;
    }

    now = () => {
        return new Date().getTime();
    }

    // Function func gets executed (without arguments) if no other funcion is executed for a certain timespan.
    // Otherwise the function is remembered and executed when the time is elapsed.
    // If other calls are done in the meantime, only the last function gets executed when time is elapsed.
    // If an other function call is just finished, this call will execute immediately.
    execute = (func) => {
        let self = this;

        if (self.lastTime == null) {
            self.executeNow(func);
        }
        else {
            let waitTime = self.lastTime + self.timespan - self.now();
            if (waitTime < 0) {
                self.executeNow(func);
            }
            else {
                if (self.functionHolder == null) {
                    self.functionHolder = func;
                    let executeLater = function() {
                        self.executeNow(self.functionHolder);
                        self.functionHolder = null;
                        self.lastTime = null;
                    }

                    setTimeout(executeLater, waitTime);
                }
                else {
                    self.functionHolder = func;
                }
            }
        }
    }

    executeNow = (func) => {
        let self = this;
        self.lastTime = new Date().getTime();
        func();
    }
}

// Even simpler delayer which always delays on first hit.
// Optional time reset during executing subsequent calls
export class Delayer2 {
    timespan: any // timespan in milliseconds
    resetDelay: boolean = false
    lastTimeout: any
    hitTime: any

    constructor(timespan: number, resetDelay: boolean = false) {
        this.timespan = timespan
        this.resetDelay = resetDelay
        this.lastTimeout = null
        this.hitTime = null
    }

    now() {
        return new Date().getTime()
    }

    // Function func gets executed (without arguments) after timespan.
    // If another function was already waiting, that one is canceled,
    // and this new function will be executed instead of the old one,
    // on the time the old one was scheduled (if resetDelay is false),
    // or after a fresh delay (if resetDelay is true).
    execute = (func) => {
        let self = this
        let execute_ = (func) => {
            func()
            self.lastTimeout = null
        }
        if (self.lastTimeout == null) {
            self.lastTimeout = setTimeout(execute_.bind(null, func), self.timespan)
            self.hitTime = self.now()
        }
        else {
            clearTimeout(self.lastTimeout)
            let timeLeft = 0.0
            if (self.resetDelay) {
                timeLeft = self.timespan
            } else {
                timeLeft = Math.max(0, self.timespan - (self.now() - self.hitTime))
            }
            self.lastTimeout = setTimeout(execute_.bind(null, func), timeLeft)
        }
    }
}