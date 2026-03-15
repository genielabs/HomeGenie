let gamepadLoopActive = false;

// Static Utility Class for controlling FPV motors
MotorControl = class {
  homeLevel = 50;
  moduleRef = null;
  module = null;
  busy = false;
  motionStopTimeout = null;

  constructor(module, address) {
    const adapter = module.getAdapter();
    const mr = this.moduleRef = adapter.getModuleReference(module, address);
    this.module = adapter.getModuleByRef({Domain: mr.Domain, Address: address});
  }

  level(l, backHome = true) {
    clearTimeout(this.motionStopTimeout);
    if (this.busy) return;
    this.busy = true;
    if (l < 0) l = 0;
    if (l > 100) l = 100;
    this.module.control('Control.Level', l).subscribe({
      next: () => {
        this.busy = false;
        if (backHome) {
          this.motionStopTimeout = setTimeout(() => {
            this.module.control('Control.Level', this.homeLevel);
          }, 100);
        }
      },
      error: () => this.busy = false
    });
  }

  levelInc(lv = 0.02) {
    var l = (this.module.field('Status.Level').decimalValue + lv) * 100;
    this.level(l, false);
  }

  levelDim(lv = 0.02) {
    var l = (this.module.field('Status.Level').decimalValue - lv) * 100;
    this.level(l, false);
  }
}
// static method
MotorControl.setSpeed = (module, address, speed) => {
  const adapter = module.getAdapter();
  const mr = adapter.getModuleReference(module, address);
  const parameterSet = `HomeAutomation.HomeGenie/Config/Modules.ParameterSet`;
  let speedParameter = {'Motor.Type.Servo.AngleSpeed': speed};
      adapter.apiCall(`${parameterSet}/${mr.Domain}/${address}`, speedParameter).subscribe({
          next: () => {
              console.log(`Set motor "${address}" speed to ${speed}`);
          },
          error: (e) => {
              console.erro('Error (MotorControl::setSpeed)', e);
          }
      });
};

class FpvControl extends ControllerInstance {
  // Widget Settings
  settings = {
    moduleSelect: {
      typeFilter: 'motor' // allow selecting only motor-type modules
    },
    sizeOptions: [],
    defaultSize: 'medium'
  };

  mini = null;

  armBase = null;
  armElbow = null;
  armWrist = null;
  armGripper = null;

  motionControl = null;
  steerControl = null;

  carLight1 = null; // ESP32 board LED
  carLight2 = null; // ESP32 camera LED

  busy = false;

  onInit() {

  }

  onCreate() {

    gamepadLoopActive = true;
    this.updateGamepadStatus();

    zuix.context(this.field('dashboard'), (ctx) => {
        console.log(ctx, this.boundModule);
        ctx.setModule(this.boundModule);
    });

    this.declare({
      getName: () => {
        return this.mini?.name || 'not_set';
      },
      isConfigured: () => {
        return this.boundModule != null;
      },
      config: () => {
        if (!this.boundModule) {
          this.configure();
        }
      }
    });

    // the bound module selected by the user
    if (this.boundModule == null) return;
    const bm = this.boundModule;

    // Motion and Steer control modules
    this.motionControl = new MotorControl(bm, 'MT1');
    this.steerControl = new MotorControl(bm, 'ST1');
    // Set wheels speed
    MotorControl.setSpeed(bm, 'S1', 8);
    MotorControl.setSpeed(bm, 'S2', 8);
    MotorControl.setSpeed(bm, 'S3', 8);
    MotorControl.setSpeed(bm, 'S4', 8);

    // Robotic Arm modules
    this.armBase = new MotorControl(bm, 'S8');
    this.armElbow = new MotorControl(bm, 'S7');
    this.armWrist = new MotorControl(bm, 'S6');
    this.armGripper = new MotorControl(bm, 'S5');
    // Set arm motors speed
    MotorControl.setSpeed(bm, 'S5', 7);
    MotorControl.setSpeed(bm, 'S6', 7);
    MotorControl.setSpeed(bm, 'S7', 7);
    MotorControl.setSpeed(bm, 'S8', 7);

    // While "Motor" modules belongs to the "Automation.Components" domain,
    // the main "mini" module always comes with the standard "HomeAutomation.HomeGenie" domain.
    // The first part of the domain is a dynamic string identifying the phisycal ESP32
    // device in HomeGenie MQTT network.
    let homegenieDomain = this.motionControl.moduleRef.Domain.replace('.Automation.Components', '.HomeAutomation.HomeGenie');
    const adapter = bm.getAdapter();
    this.mini = adapter.getModuleByRef({Domain: homegenieDomain, Address: 'mini'});
    this.carLight1 = adapter.getModuleByRef({Domain: homegenieDomain, Address: 'C1'});
    // TODO: this.carLight2 not implemented (must find a way to associate the camera module)

  }

  onDispose() {
    gamepadLoopActive = false;
    //if (this.cameraImage) this.cameraImage.onload = null;
  }

  armPreset1() {
    this.armBase.level(50, false);
    this.armElbow.level(50, false);
    this.armWrist.level(92, false);
    //this.armGripper.level(50, false);
  }
  armPreset2() {
    this.armBase.level(50, false);
    this.armElbow.level(95, false);
    this.armWrist.level(65, false);
    //this.armGripper.level(50, false);
  }
  toggleLight1() {
    if (this.busy) return;
    this.busy = true;
    this.carLight1.control('Control.Toggle').subscribe(() =>{
      setTimeout(() => this.busy = false, 200);
    });
  }
  toggleLight2() {
    // TODO:
  }

  updateGamepadStatus() {
    if (!gamepadLoopActive) {
      //console.log('GamePad loop exit.');
      return;
    }
    const gamepads = navigator.getGamepads ? navigator.getGamepads() : [];
    for (let i = 0; i < gamepads.length; i++) {
        const gp = gamepads[i];

        if (gp) {

            gp.buttons.forEach((button, index) => {

                if (button.pressed) {

                    if (index === 4) {
                      this.armWrist.levelDim();
                    }

                    if (index === 6) {
                      this.armWrist.levelInc();
                    }

                    if (index === 5) {
                      this.armGripper.levelDim(0.1);
                    }

                    if (index === 7) {
                      this.armGripper.levelInc(0.1);
                    }

                }

                if (button.pressed && !this.busy) {

                    if (index === 0) {
                      this.armPreset1();
                    }
                    if (index === 1) {
                      this.armPreset2();
                    }
                    if (index === 2) {
                      this.toggleLight1();
                    }
                    if (index === 3) {
                      this.toggleLight2();
                    }

                    if (index === 12) {
                        this.armElbow.levelDim();
                    }
                    if (index === 13) {
                        this.armElbow.levelInc();
                    }

                    if (index === 14) {
                      this.armBase.levelInc(0.1);
                    }
                    if (index === 15) {
                      this.armBase.levelDim(0.1);
                    }

                }
            });

            const threshold = 0.025;
            if (Math.abs(gp.axes[0]) > threshold) {

                const l = (Math.round((gp.axes[0] / 2) * 100) + 50);
                this.steerControl.level(l);

            }
            if (Math.abs(gp.axes[1]) > threshold) {

                const l = 100 - (Math.round((gp.axes[1] / 2) * 100) + 50);
                this.motionControl.level(l);

            }
            if (Math.abs(gp.axes[2]) > threshold) {

                const l = (Math.round((gp.axes[2] / 2 / 3) * 100) + 50);
                this.steerControl.level(l);

            }
            if (Math.abs(gp.axes[3]) > threshold) {

                const l = 100 - (Math.round((gp.axes[3] / 2 / 3) * 100) + 50);
                this.motionControl.level(l);

            }
        }
    }
    requestAnimationFrame(() => this.updateGamepadStatus());
  }
}
