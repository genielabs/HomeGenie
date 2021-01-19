import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ReteModule } from "rete-angular-render-plugin";
import { NumberNgControl } from "./controls/num.component";
import { ReteComponent } from "./rete.component";

@NgModule({
  declarations: [ReteComponent, NumberNgControl],
  imports: [CommonModule, ReteModule],
  exports: [ReteComponent, ReteModule],
  entryComponents: [NumberNgControl]
})
export class MyReteEditorModule {}
